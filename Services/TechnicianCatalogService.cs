using Fixawyy.Model_Views;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public interface ITechnicianCatalogService
{
    Task<TechnicianCatalogViewModel> SearchAsync(TechnicianCatalogFilter filter);
}

/// <summary>
/// Public technician catalog with price / rating / availability filters.
/// Complements the existing ITechnicianService.GetDirectoryAsync (which is left untouched) and reuses its card view model.
/// </summary>
public class TechnicianCatalogService : ITechnicianCatalogService
{
    private readonly Context.AppDbContext _context;

    public TechnicianCatalogService(Context.AppDbContext context)
    {
        _context = context;
    }

    public async Task<TechnicianCatalogViewModel> SearchAsync(TechnicianCatalogFilter filter)
    {
        var query = _context.TechnicianProfiles
            .AsNoTracking()
            .Include(tp => tp.User)
            .Include(tp => tp.Schedules)
            .Include(tp => tp.TechnicianSpecialties).ThenInclude(ts => ts.Specialty)
            .Where(tp => tp.User.IsActive);

        if (filter.OnlyAvailable) query = query.Where(tp => tp.IsAvailable);

        if (filter.SpecialtyId is > 0)
            query = query.Where(tp => tp.TechnicianSpecialties.Any(ts => ts.SpecialtyId == filter.SpecialtyId));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim().ToLower();
            query = query.Where(tp =>
                tp.User.FullName.ToLower().Contains(s) ||
                (tp.Bio != null && tp.Bio.ToLower().Contains(s)) ||
                tp.TechnicianSpecialties.Any(ts => ts.Specialty.Name.ToLower().Contains(s)));
        }

        if (filter.MinPrice.HasValue) query = query.Where(tp => tp.HourlyRate >= filter.MinPrice.Value);
        if (filter.MaxPrice.HasValue) query = query.Where(tp => tp.HourlyRate <= filter.MaxPrice.Value);
        if (filter.MinRating.HasValue) query = query.Where(tp => tp.Rating >= filter.MinRating.Value);

        var list = await query.OrderByDescending(tp => tp.Rating).ThenBy(tp => tp.HourlyRate).ToListAsync();
        var specialties = await _context.Specialties.AsNoTracking().OrderBy(s => s.Name).ToListAsync();

        var cards = list.Select(tp =>
        {
            var working = tp.Schedules.Where(s => s.IsWorkingDay && s.StartTime < s.EndTime).OrderBy(s => ((int)s.DayOfWeek + 1) % 7).ToList();
            var first = working.FirstOrDefault();

            return new CatalogTechnicianCardViewModel
            {
                Id = tp.Id,
                FullName = tp.User.FullName,
                ProfilePictureUrl = tp.User.ProfilePictureUrl,
                PhoneNumber = null,
                Bio = tp.Bio,
                HourlyRate = tp.HourlyRate,
                YearsOfExperience = tp.YearsOfExperience,
                IsAvailable = tp.IsAvailable,
                Rating = tp.Rating,
                ReviewCount = tp.ReviewCount,
                Specialties = tp.TechnicianSpecialties.Select(ts => ts.Specialty.Name).ToList(),
                WorkingDays = working.Select(s => s.DayOfWeek.ToString()[..3]).ToList(),
                WorkingHours = first == null
                    ? string.Empty
                    : $"{DateTime.Today.Add(first.StartTime):hh:mm tt} - {DateTime.Today.Add(first.EndTime):hh:mm tt}"
            };
        }).ToList();

        return new TechnicianCatalogViewModel { Technicians = cards, Specialties = specialties, Filter = filter };
    }
}
