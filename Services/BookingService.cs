using System.Data;
using System.Globalization;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public enum BookingOutcome
{
    Success,
    NotFound,
    Forbidden,
    Invalid
}

public class BookingResult
{
    public BookingOutcome Outcome { get; init; }
    public string? Error { get; init; }
    public int? BookingId { get; init; }
    public bool Success => Outcome == BookingOutcome.Success;

    public static BookingResult Ok(int? bookingId = null) => new() { Outcome = BookingOutcome.Success, BookingId = bookingId };
    public static BookingResult Invalid(string error) => new() { Outcome = BookingOutcome.Invalid, Error = error };
    public static BookingResult NotFound(string error = "Booking not found.") => new() { Outcome = BookingOutcome.NotFound, Error = error };
}

public interface IBookingService
{
    Task<CreateBookingViewModel?> GetBookingFormAsync(int technicianId);
    Task<SlotsResponse> GetAvailableSlotsAsync(int technicianId, DateTime date, int serviceCount);
    Task<BookingResult> CreateBookingAsync(string customerId, CreateBookingViewModel model);
    Task<List<BookingListItemViewModel>> GetCustomerBookingsAsync(string customerId);
    Task<BookingListItemViewModel?> GetCustomerBookingAsync(string customerId, int bookingId);
    Task<List<BookingListItemViewModel>> GetTechnicianBookingsAsync(int technicianProfileId);
    Task<BookingResult> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, BookingActor actor);
}

/// <summary>
/// Booking lifecycle + appointment scheduling rules (Teammate 2).
/// "Active" bookings (Pending / Confirmed / Completed) block the technician's time; Cancelled ones do not.
/// </summary>
public class BookingService : IBookingService
{
    // Business rules, in one place.
    public const int SlotMinutes = 60;            // each selected service = one hour of technician time
    public const int MaxServicesPerBooking = 4;   // => longest booking is 4 hours
    public const int MinLeadMinutes = 60;         // cannot book less than 1 hour ahead
    public const int MaxAdvanceDays = 60;         // cannot book more than 60 days ahead
    public const int CustomerCancelCutoffHours = 2; // customers cannot cancel within 2 hours of the start

    private readonly Context.AppDbContext _context;
    private readonly IBookingClock _clock;

    public BookingService(Context.AppDbContext context, IBookingClock clock)
    {
        _context = context;
        _clock = clock;
    }

    // ------------------------------------------------------------------ booking form

    public async Task<CreateBookingViewModel?> GetBookingFormAsync(int technicianId)
    {
        var tech = await _context.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.TechnicianSpecialties).ThenInclude(ts => ts.Specialty)
            .FirstOrDefaultAsync(t => t.Id == technicianId);

        if (tech == null || !tech.User.IsActive) return null;

        return BuildFormModel(tech, new CreateBookingViewModel { TechnicianId = technicianId });
    }

    private static CreateBookingViewModel BuildFormModel(TechnicianProfile tech, CreateBookingViewModel model)
    {
        model.TechnicianId = tech.Id;
        model.TechnicianName = tech.User.FullName;
        model.ProfilePictureUrl = tech.User.ProfilePictureUrl;
        model.HourlyRate = tech.HourlyRate;
        model.Rating = tech.Rating;
        model.ReviewCount = tech.ReviewCount;
        model.MaxAdvanceDays = MaxAdvanceDays;
        model.Services = tech.TechnicianSpecialties
            .Select(ts => new ServiceOptionViewModel
            {
                SpecialtyId = ts.SpecialtyId,
                Name = ts.Specialty.Name,
                Description = ts.Specialty.Description
            })
            .OrderBy(s => s.Name)
            .ToList();
        return model;
    }

    // ------------------------------------------------------------------ availability

    public async Task<SlotsResponse> GetAvailableSlotsAsync(int technicianId, DateTime date, int serviceCount)
    {
        date = date.Date;
        var durationMinutes = Math.Clamp(serviceCount, 1, MaxServicesPerBooking) * SlotMinutes;

        var (tech, error) = await LoadBookableTechnicianAsync(technicianId);
        if (tech == null) return new SlotsResponse { Success = false, Message = error };

        var dateError = ValidateDate(date);
        if (dateError != null) return new SlotsResponse { Success = false, Message = dateError };

        var window = GetWorkingWindow(tech, date);
        if (window == null)
        {
            return new SlotsResponse { Success = true, Message = "The technician does not work on this day. Please pick another date." };
        }

        var booked = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.TechnicianId == technicianId && b.BookingDate == date && b.Status != BookingStatus.Cancelled)
            .Select(b => new { b.StartTime, b.EndTime })
            .ToListAsync();

        var now = _clock.Now;
        var slots = new List<TimeSlotViewModel>();
        var step = TimeSpan.FromMinutes(SlotMinutes);
        var length = TimeSpan.FromMinutes(durationMinutes);

        for (var start = window.Value.Start; start + length <= window.Value.End; start += step)
        {
            var end = start + length;

            if (date.Add(start) < now.AddMinutes(MinLeadMinutes)) continue;
            if (booked.Any(b => Overlaps(start, end, b.StartTime, b.EndTime))) continue;

            slots.Add(new TimeSlotViewModel
            {
                Value = start.ToString(@"hh\:mm"),
                Label = $"{FormatTime(start)} - {FormatTime(end)}"
            });
        }

        return new SlotsResponse
        {
            Success = true,
            Slots = slots,
            Message = slots.Count == 0 ? "No free time slots on this date. Please try another day." : null
        };
    }

    // ------------------------------------------------------------------ create

    public async Task<BookingResult> CreateBookingAsync(string customerId, CreateBookingViewModel model)
    {
        // 1. Basic input
        if (model.BookingDate == null) return BookingResult.Invalid("Please choose a date.");
        if (!TimeSpan.TryParseExact(model.StartTime ?? string.Empty, @"hh\:mm", CultureInfo.InvariantCulture, out var start))
            return BookingResult.Invalid("Please choose a valid time slot.");

        var date = model.BookingDate.Value.Date;

        // 2. Technician must exist, be active and available
        var (tech, techError) = await LoadBookableTechnicianAsync(model.TechnicianId);
        if (tech == null) return BookingResult.Invalid(techError ?? "Technician not found.");

        // 3. Services must be offered by this technician
        var selectedIds = model.SelectedSpecialtyIds.Distinct().ToList();
        if (selectedIds.Count == 0) return BookingResult.Invalid("Please select at least one service.");
        if (selectedIds.Count > MaxServicesPerBooking)
            return BookingResult.Invalid($"You can book at most {MaxServicesPerBooking} services at once.");

        var offered = tech.TechnicianSpecialties.ToDictionary(ts => ts.SpecialtyId, ts => ts.Specialty);
        if (selectedIds.Any(id => !offered.ContainsKey(id)))
            return BookingResult.Invalid("One or more selected services are not offered by this technician.");

        // 4. Date rules
        var dateError = ValidateDate(date);
        if (dateError != null) return BookingResult.Invalid(dateError);

        var duration = TimeSpan.FromMinutes(selectedIds.Count * SlotMinutes);
        var end = start + duration;

        if (date.Add(start) < _clock.Now.AddMinutes(MinLeadMinutes))
            return BookingResult.Invalid("This time is too soon. Bookings must be made at least 1 hour in advance.");

        // 5. Working schedule rules
        var window = GetWorkingWindow(tech, date);
        if (window == null)
            return BookingResult.Invalid($"{tech.User.FullName} does not work on {date:dddd}.");
        if (start < window.Value.Start || end > window.Value.End)
            return BookingResult.Invalid($"The requested time is outside the technician's working hours ({FormatTime(window.Value.Start)} - {FormatTime(window.Value.End)}).");
        if ((start - window.Value.Start).TotalMinutes % SlotMinutes != 0)
            return BookingResult.Invalid("Please choose one of the listed time slots.");

        // 6. Customer rules
        var customer = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == customerId);
        if (customer == null || !customer.IsActive) return BookingResult.Invalid("Your account could not be found or is inactive.");
        if (customer.Id == tech.UserId) return BookingResult.Invalid("You cannot book yourself.");

        // 7. Overlap check + insert, serialised per technician to avoid double booking
        var canUseTransaction = _context.Database.IsRelational();
        var tx = canUseTransaction ? await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted) : null;
        try
        {
            if (tx != null) await DbLock.AcquireAsync(_context, $"booking-tech-{tech.Id}");

            var hasOverlap = await _context.Bookings.AnyAsync(b =>
                b.TechnicianId == tech.Id &&
                b.BookingDate == date &&
                b.Status != BookingStatus.Cancelled &&
                b.StartTime < end && start < b.EndTime);

            if (hasOverlap)
            {
                return BookingResult.Invalid("This time slot is no longer available. Please choose another one.");
            }

            var booking = new Booking
            {
                CustomerId = customerId,
                TechnicianId = tech.Id,
                BookingDate = date,
                StartTime = start,
                EndTime = end,
                Status = BookingStatus.Pending,
                Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var id in selectedIds)
            {
                booking.Items.Add(new BookingItem
                {
                    SpecialtyId = id,
                    ServiceName = offered[id].Name,
                    Price = tech.HourlyRate * SlotMinutes / 60m,
                    DurationMinutes = SlotMinutes
                });
            }
            booking.TotalPrice = booking.Items.Sum(i => i.Price);

            _context.Bookings.Add(booking);
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
            // Most likely the unique "active slot" index fired: someone else took the slot.
            _context.ChangeTracker.Clear();
            return BookingResult.Invalid("This time slot was just taken by another customer. Please choose another one.");
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    // ------------------------------------------------------------------ queries

    public async Task<List<BookingListItemViewModel>> GetCustomerBookingsAsync(string customerId)
    {
        var bookings = await BookingQuery()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.StartTime)
            .ToListAsync();

        return bookings.Select(b => MapForCustomer(b)).ToList();
    }

    public async Task<BookingListItemViewModel?> GetCustomerBookingAsync(string customerId, int bookingId)
    {
        var booking = await BookingQuery()
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId);

        return booking == null ? null : MapForCustomer(booking);
    }

    public async Task<List<BookingListItemViewModel>> GetTechnicianBookingsAsync(int technicianProfileId)
    {
        var bookings = await BookingQuery()
            .Where(b => b.TechnicianId == technicianProfileId)
            .OrderByDescending(b => b.BookingDate).ThenByDescending(b => b.StartTime)
            .ToListAsync();

        return bookings.Select(b => MapForTechnician(b)).ToList();
    }

    // ------------------------------------------------------------------ status updates

    public async Task<BookingResult> UpdateBookingStatusAsync(int bookingId, BookingStatus newStatus, BookingActor actor)
    {
        var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return BookingResult.NotFound();

        var isOwnerTechnician = actor.IsTechnician && actor.TechnicianProfileId == booking.TechnicianId;
        var isOwnerCustomer = actor.IsCustomer && booking.CustomerId == actor.UserId;

        // Strangers get "not found" so booking ids cannot be probed.
        if (!isOwnerTechnician && !isOwnerCustomer) return BookingResult.NotFound();

        var now = _clock.Now;
        if (isOwnerTechnician)
        {
            if (!TechnicianTransitions(booking, now).Contains(newStatus))
                return BookingResult.Invalid(DescribeInvalidTransition(booking.Status, newStatus, true));
        }
        else
        {
            if (newStatus != BookingStatus.Cancelled)
                return new BookingResult { Outcome = BookingOutcome.Forbidden, Error = "Customers can only cancel bookings." };

            if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
                return BookingResult.Invalid($"A {booking.Status.ToString().ToLower()} booking cannot be cancelled.");

            if (!CanCustomerCancel(booking, now))
                return BookingResult.Invalid($"Bookings cannot be cancelled within {CustomerCancelCutoffHours} hours of the start time. Please contact the technician.");
        }

        booking.Status = newStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return BookingResult.Invalid("This booking was changed by someone else. Please refresh and try again.");
        }

        return BookingResult.Ok(booking.Id);
    }

    // ------------------------------------------------------------------ rules (pure functions)

    /// <summary>Time-range overlap: [aStart, aEnd) intersects [bStart, bEnd).</summary>
    public static bool Overlaps(TimeSpan aStart, TimeSpan aEnd, TimeSpan bStart, TimeSpan bEnd)
        => aStart < bEnd && bStart < aEnd;

    private bool CanCustomerCancel(Booking b, DateTime now)
        => b.Status is BookingStatus.Pending or BookingStatus.Confirmed
           && b.BookingDate.Date.Add(b.StartTime) >= now.AddHours(CustomerCancelCutoffHours);

    /// <summary>Allowed lifecycle for technicians: Pending -> Confirmed/Cancelled, Confirmed -> Completed/Cancelled.</summary>
    private List<BookingStatus> TechnicianTransitions(Booking b, DateTime now)
    {
        var list = new List<BookingStatus>();
        switch (b.Status)
        {
            case BookingStatus.Pending:
                list.Add(BookingStatus.Confirmed);
                list.Add(BookingStatus.Cancelled);
                break;
            case BookingStatus.Confirmed:
                // A job can only be completed once its start time has been reached.
                if (b.BookingDate.Date.Add(b.StartTime) <= now) list.Add(BookingStatus.Completed);
                list.Add(BookingStatus.Cancelled);
                break;
        }
        return list;
    }

    private static string DescribeInvalidTransition(BookingStatus from, BookingStatus to, bool isTechnician)
    {
        if (from is BookingStatus.Completed or BookingStatus.Cancelled)
            return $"This booking is already {from.ToString().ToLower()} and cannot be changed.";
        if (from == BookingStatus.Confirmed && to == BookingStatus.Completed)
            return "A booking can only be marked as completed after its start time.";
        return $"A {from.ToString().ToLower()} booking cannot be changed to {to.ToString().ToLower()}.";
    }

    private string? ValidateDate(DateTime date)
    {
        var today = _clock.Now.Date;
        if (date < today) return "The requested date is in the past.";
        if (date > today.AddDays(MaxAdvanceDays)) return $"Bookings can only be made up to {MaxAdvanceDays} days in advance.";
        return null;
    }

    /// <summary>Returns the technician's working window for that date, or null when not working.</summary>
    private static (TimeSpan Start, TimeSpan End)? GetWorkingWindow(TechnicianProfile tech, DateTime date)
    {
        var schedule = tech.Schedules.FirstOrDefault(s => s.DayOfWeek == date.DayOfWeek);
        if (schedule == null || !schedule.IsWorkingDay || schedule.StartTime >= schedule.EndTime) return null;
        return (schedule.StartTime, schedule.EndTime);
    }

    private async Task<(TechnicianProfile? Tech, string? Error)> LoadBookableTechnicianAsync(int technicianId)
    {
        var tech = await _context.TechnicianProfiles
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Schedules)
            .Include(t => t.TechnicianSpecialties).ThenInclude(ts => ts.Specialty)
            .FirstOrDefaultAsync(t => t.Id == technicianId);

        if (tech == null || !tech.User.IsActive) return (null, "Technician not found.");
        if (!tech.IsAvailable) return (null, $"{tech.User.FullName} is currently not accepting bookings.");
        return (tech, null);
    }

    // ------------------------------------------------------------------ mapping

    private IQueryable<Booking> BookingQuery() => _context.Bookings
        .AsNoTracking()
        .Include(b => b.Items)
        .Include(b => b.Review)
        .Include(b => b.Customer)
        .Include(b => b.Technician).ThenInclude(t => t.User);

    private BookingListItemViewModel MapForCustomer(Booking b)
    {
        var vm = Map(b);
        vm.CanCancel = CanCustomerCancel(b, _clock.Now);
        vm.CanReview = b.Status == BookingStatus.Completed && b.Review == null;
        return vm;
    }

    private BookingListItemViewModel MapForTechnician(Booking b)
    {
        var vm = Map(b);
        vm.AllowedNextStatuses = TechnicianTransitions(b, _clock.Now);
        return vm;
    }

    private static BookingListItemViewModel Map(Booking b) => new()
    {
        Id = b.Id,
        TechnicianId = b.TechnicianId,
        TechnicianName = b.Technician.User.FullName,
        TechnicianPictureUrl = b.Technician.User.ProfilePictureUrl,
        TechnicianPhone = b.Technician.User.PhoneNumber,
        CustomerName = b.Customer.FullName,
        CustomerPhone = b.Customer.PhoneNumber,
        BookingDate = b.BookingDate,
        StartTime = b.StartTime,
        EndTime = b.EndTime,
        Status = b.Status,
        Notes = b.Notes,
        TotalPrice = b.TotalPrice,
        CreatedAt = b.CreatedAt,
        HasReview = b.Review != null,
        Items = b.Items.Select(i => new BookingItemViewModel { ServiceName = i.ServiceName, Price = i.Price }).ToList()
    };

    private static string FormatTime(TimeSpan t) => DateTime.Today.Add(t).ToString("hh:mm tt", CultureInfo.InvariantCulture);
}
