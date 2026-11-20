using System.Data;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public interface IReviewService
{
    /// <summary>Data for the review form; null if the booking does not belong to the customer.</summary>
    Task<ReviewFormViewModel?> GetReviewFormAsync(int bookingId, string customerId);
    Task<BookingResult> SubmitReviewAsync(string customerId, ReviewFormViewModel model);
    Task<TechnicianReviewsViewModel?> GetTechnicianReviewsAsync(int technicianId);
    /// <summary>Average of all written reviews for the technician (0 when there are none).</summary>
    Task<double> CalculateAverageRatingAsync(int technicianId);
}

public class ReviewService : IReviewService
{
    private readonly Context.AppDbContext _context;

    public ReviewService(Context.AppDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewFormViewModel?> GetReviewFormAsync(int bookingId, string customerId)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Items)
            .Include(b => b.Technician).ThenInclude(t => t.User)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        if (booking == null) return null;

        return new ReviewFormViewModel
        {
            BookingId = booking.Id,
            TechnicianName = booking.Technician.User.FullName,
            TechnicianPictureUrl = booking.Technician.User.ProfilePictureUrl,
            BookingDate = booking.BookingDate,
            ServicesText = string.Join(", ", booking.Items.Select(i => i.ServiceName))
        };
    }

    public async Task<BookingResult> SubmitReviewAsync(string customerId, ReviewFormViewModel model)
    {
        if (model.Rating is null or < 1 or > 5)
            return BookingResult.Invalid("Rating must be between 1 and 5 stars.");

        var comment = string.IsNullOrWhiteSpace(model.Comment) ? null : model.Comment.Trim();
        if (comment is { Length: > 1000 })
            return BookingResult.Invalid("Comment cannot exceed 1000 characters.");

        // The booking must exist and belong to this customer (otherwise "not found": no probing).
        var booking = await _context.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.CustomerId == customerId);
        if (booking == null) return BookingResult.NotFound("Booking not found.");

        var tx = _context.Database.IsRelational() ? await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null;
        try
        {
            // Serialise reviews of the same technician so the running average is never overwritten.
            if (tx != null) await DbLock.AcquireAsync(_context, $"review-tech-{booking.TechnicianId}");

            // Re-read inside the lock: status and "already reviewed" checks must be current.
            var status = await _context.Bookings
                .Where(b => b.Id == booking.Id)
                .Select(b => b.Status)
                .FirstAsync();
            if (status != BookingStatus.Completed)
                return BookingResult.Invalid("You can only review a booking after the service has been completed.");

            if (await _context.Reviews.AnyAsync(r => r.BookingId == booking.Id))
                return BookingResult.Invalid("You have already reviewed this booking.");

            var technician = await _context.TechnicianProfiles.FirstOrDefaultAsync(t => t.Id == booking.TechnicianId);
            if (technician == null) return BookingResult.Invalid("Technician not found.");

            _context.Reviews.Add(new Review
            {
                BookingId = booking.Id,
                CustomerId = customerId,
                TechnicianId = booking.TechnicianId,
                Rating = model.Rating.Value,
                Comment = comment,
                CreatedAt = DateTime.UtcNow
            });

            // Running average. TechnicianProfile may already carry a rating/count (seed data or earlier
            // reviews), so we fold the new rating into it instead of recomputing from scratch.
            var newCount = technician.ReviewCount + 1;
            var newAverage = ((technician.Rating * technician.ReviewCount) + model.Rating.Value) / newCount;
            technician.Rating = Math.Round(Math.Clamp(newAverage, 0.0, 5.0), 2);
            technician.ReviewCount = newCount;

            await _context.SaveChangesAsync();
            if (tx != null) await tx.CommitAsync();
            return BookingResult.Ok(booking.Id);
        }
        catch (TimeoutException ex)
        {
            return BookingResult.Invalid(ex.Message);
        }
        catch (DbUpdateException)
        {
            // Unique index on Reviews.BookingId: a parallel request already saved a review.
            _context.ChangeTracker.Clear();
            return BookingResult.Invalid("You have already reviewed this booking.");
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    public async Task<TechnicianReviewsViewModel?> GetTechnicianReviewsAsync(int technicianId)
    {
        var tech = await _context.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.TechnicianSpecialties).ThenInclude(ts => ts.Specialty)
            .FirstOrDefaultAsync(t => t.Id == technicianId);

        if (tech == null || !tech.User.IsActive) return null;

        var reviews = await _context.Reviews
            .AsNoTracking()
            .Where(r => r.TechnicianId == technicianId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.Rating, r.Comment, r.CreatedAt, r.Customer.FullName })
            .ToListAsync();

        var starCounts = new int[6];
        foreach (var r in reviews) starCounts[r.Rating]++;

        return new TechnicianReviewsViewModel
        {
            TechnicianId = tech.Id,
            TechnicianName = tech.User.FullName,
            ProfilePictureUrl = tech.User.ProfilePictureUrl,
            HourlyRate = tech.HourlyRate,
            IsAvailable = tech.IsAvailable,
            Specialties = tech.TechnicianSpecialties.Select(ts => ts.Specialty.Name).ToList(),
            AverageRating = tech.Rating,
            ReviewCount = tech.ReviewCount,
            StarCounts = starCounts,
            Reviews = reviews.Take(50).Select(r => new ReviewItemViewModel
            {
                Id = r.Id,
                CustomerName = ToPrivateName(r.FullName),
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList()
        };
    }

    public async Task<double> CalculateAverageRatingAsync(int technicianId)
    {
        var ratings = _context.Reviews.AsNoTracking().Where(r => r.TechnicianId == technicianId);
        if (!await ratings.AnyAsync()) return 0.0;
        return Math.Round(await ratings.AverageAsync(r => (double)r.Rating), 2);
    }

    /// <summary>Public pages show "Sara M." instead of the customer's full name.</summary>
    private static string ToPrivateName(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "Customer";
        return parts.Length == 1 ? parts[0] : $"{parts[0]} {parts[1][0]}.";
    }
}
