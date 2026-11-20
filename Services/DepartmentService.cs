using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Services;

public sealed class DepartmentService : IDepartmentService
{
    private readonly AppDbContext _context;

    public DepartmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<DepartmentDetailsViewModel?> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
        {
            return null;
        }

        var requests = await _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.DepartmentId == id)
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

        return new DepartmentDetailsViewModel
        {
            Department = department,
            Requests = requests
        };
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Departments.AnyAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<(bool Succeeded, string? Error)> CreateAsync(
        DepartmentFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var name = NormalizeName(model.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Department name is required.");
        }

        if (await NameExistsAsync(name, null, cancellationToken))
        {
            return (false, "A department with this name already exists.");
        }

        _context.Departments.Add(new Department
        {
            Name = name,
            Description = NormalizeOptional(model.Description)
        });

        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> UpdateAsync(
        DepartmentFormViewModel model,
        CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == model.Id, cancellationToken);

        if (department == null)
        {
            return (false, "Department was not found.");
        }

        var name = NormalizeName(model.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Department name is required.");
        }

        if (await NameExistsAsync(name, model.Id, cancellationToken))
        {
            return (false, "A department with this name already exists.");
        }

        department.Name = name;
        department.Description = NormalizeOptional(model.Description);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        if (department == null)
        {
            return (false, "Department was not found.");
        }

        if (await _context.MaintenanceTickets.AnyAsync(t => t.DepartmentId == id, cancellationToken))
        {
            return (false, "This department cannot be deleted because it has maintenance requests.");
        }

        _context.Departments.Remove(department);
        await _context.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private Task<bool> NameExistsAsync(string name, int? excludedId, CancellationToken cancellationToken)
    {
        var query = _context.Departments
            .Where(d => d.Name.ToLower() == name.ToLower());

        if (excludedId.HasValue)
        {
            query = query.Where(d => d.Id != excludedId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    private static string NormalizeName(string? value) => value?.Trim() ?? string.Empty;

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
