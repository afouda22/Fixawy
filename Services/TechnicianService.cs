using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public interface ITechnicianService
{
    Task<TechnicianProfileViewModel?> GetProfileByUserIdAsync(string userId);
    Task<bool> UpdateProfileAsync(string userId, TechnicianProfileViewModel model);
    Task<TechnicianScheduleViewModel> GetScheduleByUserIdAsync(string userId);
    Task<bool> UpdateScheduleAsync(string userId, TechnicianScheduleViewModel model);
    Task<TechnicianDirectoryViewModel> GetDirectoryAsync(int? specialtyId, string? search);
    Task<bool> UpdateRatingAsync(int technicianId, double newRating, int? totalReviews = null);
}

public class TechnicianService : ITechnicianService
{
    private readonly Context.AppDbContext _context;

    public TechnicianService(Context.AppDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicianProfileViewModel?> GetProfileByUserIdAsync(string userId)
    {
        var tp = await _context.TechnicianProfiles
            .Include(t => t.User)
            .Include(t => t.TechnicianSpecialties)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (tp == null) return null;

        var allSpecs = await _context.Specialties.AsNoTracking().ToListAsync();

        return new TechnicianProfileViewModel
        {
            Id = tp.Id,
            FullName = tp.User.FullName,
            Email = tp.User.Email ?? string.Empty,
            PhoneNumber = tp.User.PhoneNumber,
            Bio = tp.Bio,
            HourlyRate = tp.HourlyRate,
            YearsOfExperience = tp.YearsOfExperience,
            IsAvailable = tp.IsAvailable,
            SelectedSpecialtyIds = tp.TechnicianSpecialties.Select(ts => ts.SpecialtyId).ToList(),
            AllSpecialties = allSpecs
        };
    }

    public async Task<bool> UpdateProfileAsync(string userId, TechnicianProfileViewModel model)
    {
        var tp = await _context.TechnicianProfiles
            .Include(t => t.TechnicianSpecialties)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (tp == null)
        {
            tp = new TechnicianProfile { UserId = userId };
            await _context.TechnicianProfiles.AddAsync(tp);
        }

        tp.Bio = model.Bio;
        tp.HourlyRate = model.HourlyRate;
        tp.YearsOfExperience = model.YearsOfExperience;
        tp.IsAvailable = model.IsAvailable;

        tp.TechnicianSpecialties.Clear();
        if (model.SelectedSpecialtyIds != null && model.SelectedSpecialtyIds.Count > 0)
        {
            var validIds = await _context.Specialties
                .Where(s => model.SelectedSpecialtyIds.Contains(s.Id))
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var id in validIds)
            {
                tp.TechnicianSpecialties.Add(new TechnicianSpecialty
                {
                    TechnicianProfile = tp,
                    SpecialtyId = id
                });
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TechnicianScheduleViewModel> GetScheduleByUserIdAsync(string userId)
    {
        var tp = await _context.TechnicianProfiles
            .Include(t => t.Schedules)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        var days = Enum.GetValues<DayOfWeek>();
        var existing = tp?.Schedules.ToList() ?? new List<TechnicianSchedule>();

        var list = new List<ScheduleItemViewModel>();
        foreach (var d in days)
        {
            var found = existing.FirstOrDefault(s => s.DayOfWeek == d);
            list.Add(new ScheduleItemViewModel
            {
                Id = found?.Id ?? 0,
                DayOfWeek = d,
                StartTime = found?.StartTime.ToString(@"hh\:mm") ?? "09:00",
                EndTime = found?.EndTime.ToString(@"hh\:mm") ?? "17:00",
                IsWorkingDay = found?.IsWorkingDay ?? (d != DayOfWeek.Friday && d != DayOfWeek.Saturday)
            });
        }

        return new TechnicianScheduleViewModel { Schedules = list };
    }

    public async Task<bool> UpdateScheduleAsync(string userId, TechnicianScheduleViewModel model)
    {
        var tp = await _context.TechnicianProfiles
            .Include(t => t.Schedules)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (tp == null) return false;

        _context.TechnicianSchedules.RemoveRange(tp.Schedules);

        foreach (var item in model.Schedules)
        {
            TimeSpan.TryParse(item.StartTime, out var st);
            TimeSpan.TryParse(item.EndTime, out var et);

            tp.Schedules.Add(new TechnicianSchedule
            {
                TechnicianProfileId = tp.Id,
                DayOfWeek = item.DayOfWeek,
                StartTime = st,
                EndTime = et,
                IsWorkingDay = item.IsWorkingDay
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<TechnicianDirectoryViewModel> GetDirectoryAsync(int? specialtyId, string? search)
    {
        var query = _context.TechnicianProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.TechnicianSpecialties)
                .ThenInclude(ts => ts.Specialty)
            .Where(tp => tp.User.IsActive)
            .AsNoTracking();

        if (specialtyId.HasValue && specialtyId.Value > 0)
        {
            query = query.Where(tp => tp.TechnicianSpecialties.Any(ts => ts.SpecialtyId == specialtyId.Value));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(tp =>
                tp.User.FullName.ToLower().Contains(s) ||
                (tp.Bio != null && tp.Bio.ToLower().Contains(s)) ||
                tp.TechnicianSpecialties.Any(ts => ts.Specialty.Name.ToLower().Contains(s)));
        }

        var list = await query.OrderByDescending(tp => tp.Rating).ToListAsync();
        var specs = await _context.Specialties.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var cardList = list.Select(tp => new TechnicianCardViewModel
        {
            Id = tp.Id,
            FullName = tp.User.FullName,
            ProfilePictureUrl = tp.User.ProfilePictureUrl,
            PhoneNumber = tp.User.PhoneNumber,
            Bio = tp.Bio,
            HourlyRate = tp.HourlyRate,
            YearsOfExperience = tp.YearsOfExperience,
            IsAvailable = tp.IsAvailable,
            Rating = tp.Rating,
            ReviewCount = tp.ReviewCount,
            Specialties = tp.TechnicianSpecialties.Select(ts => ts.Specialty.Name).ToList()
        }).ToList();

        return new TechnicianDirectoryViewModel
        {
            Technicians = cardList,
            Specialties = specs,
            SelectedSpecialtyId = specialtyId,
            Search = search
        };
    }

    public async Task<bool> UpdateRatingAsync(int technicianId, double newRating, int? totalReviews = null)
    {
        var profile = await _context.TechnicianProfiles.FindAsync(technicianId);
        if (profile == null) return false;

        profile.Rating = Math.Round(Math.Clamp(newRating, 0.0, 5.0), 2);
        if (totalReviews.HasValue)
        {
            profile.ReviewCount = totalReviews.Value;
        }
        else
        {
            profile.ReviewCount++;
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
