using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public sealed class LocationService : ILocationService
{
    private readonly AppDbContext _context;

    public LocationService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Locations
            .AsNoTracking()
            .OrderBy(l => l.Name)
            .ThenBy(l => l.Building)
            .ThenBy(l => l.Floor)
            .ToListAsync(cancellationToken);
    }

    public async Task<LocationDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var location = await _context.Locations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            return null;
        }

        var requests = await _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.LocationId == id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new MaintenanceRequestListDto
            {
                Id = t.Id,
                Title = t.Title,
                DepartmentName = t.DepartmentEntity != null ? t.DepartmentEntity.Name : t.Department,
                LocationName = t.Location != null ? t.Location.Name : string.Empty,
                Building = t.Location != null ? t.Location.Building : null,
                Floor = t.Location != null ? t.Location.Floor : null,
                Priority = t.Priority.ToString(),
                PriorityScore = t.PriorityScore,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new LocationDetailsViewModel
        {
            Location = location,
            Requests = requests
        };
    }

    public async Task<(bool Succeeded, string? Error)> CreateAsync(
        LocationFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeRequired(model.Name);
        if (name.Length == 0)
        {
            return (false, "Location name is required.");
        }

        if (await NameExistsAsync(name, null, cancellationToken))
        {
            return (false, "A location with this name already exists.");
        }

        _context.Locations.Add(new Location
        {
            Name = name,
            Description = NormalizeOptional(model.Description),
            Building = NormalizeOptional(model.Building),
            Floor = NormalizeOptional(model.Floor)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        LocationFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == model.Id, cancellationToken);

        if (location == null)
        {
            return (false, "Location was not found.");
        }

        var name = NormalizeRequired(model.Name);
        if (name.Length == 0)
        {
            return (false, "Location name is required.");
        }

        if (await NameExistsAsync(name, model.Id, cancellationToken))
        {
            return (false, "A location with this name already exists.");
        }

        location.Name = name;
        location.Description = NormalizeOptional(model.Description);
        location.Building = NormalizeOptional(model.Building);
        location.Floor = NormalizeOptional(model.Floor);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location == null)
        {
            return (false, "Location was not found.");
        }

        if (await _context.MaintenanceTickets.AnyAsync(t => t.LocationId == id, cancellationToken))
        {
            return (false, "This location cannot be deleted because it has maintenance requests.");
        }

        _context.Locations.Remove(location);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private Task<bool> NameExistsAsync(string name, int? excludedId, CancellationToken cancellationToken)
    {
        var query = _context.Locations
            .Where(l => l.Name.ToLower() == name.ToLower());

        if (excludedId.HasValue)
        {
            query = query.Where(l => l.Id != excludedId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    private static string NormalizeRequired(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
