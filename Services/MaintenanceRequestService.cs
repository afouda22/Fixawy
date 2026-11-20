
using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Fixawyy.Services;

public class MaintenanceRequestService : IMaintenanceRequestService
{
    private readonly AppDbContext _context;
    private readonly PriorityCalculator _priorityCalculator;

    public MaintenanceRequestService(
        AppDbContext context,
        PriorityCalculator priorityCalculator)
    {
        _context = context;
        _priorityCalculator = priorityCalculator;
    }

    public async Task<int> CreateRequestAsync(
        CreateMaintenanceRequestDto request,
        string userId)
    {
        // Validate enum values
        if (!Enum.IsDefined(typeof(ImpactScope), request.ImpactScope) ||
            !Enum.IsDefined(typeof(HazardLevel), request.HazardLevel) ||
            !Enum.IsDefined(typeof(LocationCriticality), request.LocationCriticality))
        {
            throw new ArgumentException("Invalid prioritization values.");
        }

        // Check department
        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId);

        if (department == null)
        {
            throw new ArgumentException("Department not found.");
        }

        // Check location
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.Id == request.LocationId);

        if (location == null)
        {
            throw new ArgumentException("Location not found.");
        }

        // Calculate smart priority
        int priorityScore = _priorityCalculator.CalculateScore(
            request.ImpactScope,
            request.HazardLevel,
            request.LocationCriticality);

        TicketPriority priority =
            _priorityCalculator.CalculatePriority(priorityScore);

        // Create maintenance ticket
        var ticket = new MaintenanceTicket
        {
            Title = request.Title,
            Description = request.Description,

            // Keep legacy Department property for team compatibility
            Department = department.Name,

            DepartmentId = department.Id,
            LocationId = location.Id,

            RequiredSkill = request.RequiredSkill,

            ImpactScope = request.ImpactScope,
            HazardLevel = request.HazardLevel,
            LocationCriticality = request.LocationCriticality,

            PriorityScore = priorityScore,
            Priority = priority,

            Status = TicketStatus.Pending,

            SubmittedByUserId = userId,

            CreatedAt = DateTime.UtcNow
        };

        _context.MaintenanceTickets.Add(ticket);

        await _context.SaveChangesAsync();

        return ticket.Id;
    }

    public async Task<List<MaintenanceRequestListDto>> GetMyRequestsAsync(
        string userId)
    {
        return await _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.SubmittedByUserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new MaintenanceRequestListDto
            {
                Id = t.Id,
                Title = t.Title,

                DepartmentName = t.DepartmentEntity != null
                    ? t.DepartmentEntity.Name
                    : t.Department,

                LocationName = t.Location != null
                    ? t.Location.Name
                    : string.Empty,

                Building = t.Location != null
                    ? t.Location.Building
                    : null,

                Floor = t.Location != null
                    ? t.Location.Floor
                    : null,

                Priority = t.Priority.ToString(),
                PriorityScore = t.PriorityScore,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<MaintenanceRequestDetailsDto?> GetRequestByIdAsync(
        int requestId,
        string userId)
    {
        return await _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.Id == requestId &&
                        t.SubmittedByUserId == userId)
            .Select(t => new MaintenanceRequestDetailsDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,

                DepartmentName = t.DepartmentEntity != null
                    ? t.DepartmentEntity.Name
                    : t.Department,

                LocationName = t.Location != null
                    ? t.Location.Name
                    : string.Empty,

                Building = t.Location != null
                    ? t.Location.Building
                    : null,

                Floor = t.Location != null
                    ? t.Location.Floor
                    : null,

                RequiredSkill = t.RequiredSkill,

                ImpactScope = t.ImpactScope.HasValue
                    ? t.ImpactScope.Value.ToString()
                    : string.Empty,

                HazardLevel = t.HazardLevel.HasValue
                    ? t.HazardLevel.Value.ToString()
                    : string.Empty,

                LocationCriticality = t.LocationCriticality.HasValue
                    ? t.LocationCriticality.Value.ToString()
                    : string.Empty,

                Priority = t.Priority.ToString(),
                PriorityScore = t.PriorityScore,
                Status = t.Status.ToString(),

                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                ResolvedAt = t.ResolvedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MaintenanceRequestListPageViewModel> GetRequestsAsync(
        string userId,
        bool canViewAll,
        MaintenanceRequestFilterViewModel filter,
        CancellationToken cancellationToken = default)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, 100);

        var query = _context.MaintenanceTickets.AsNoTracking().AsQueryable();
        if (!canViewAll)
        {
            query = query.Where(t => t.SubmittedByUserId == userId);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == filter.Priority.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(t => t.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(t => t.LocationId == filter.LocationId.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value.Date);
        }

        if (filter.ToDate.HasValue)
        {
            var exclusiveEnd = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(t => t.CreatedAt < exclusiveEnd);
        }

        query = filter.Sort.ToLowerInvariant() switch
        {
            "priority" => query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt),
            "oldest" => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var requests = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(ListDtoProjection)
            .ToListAsync(cancellationToken);

        return new MaintenanceRequestListPageViewModel
        {
            Requests = requests,
            Filter = filter,
            TotalCount = totalCount
        };
    }

    public async Task<MaintenanceRequestDetailsPageViewModel?> GetDetailsForUserAsync(
        int requestId,
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.Id == requestId);

        if (!canViewAll)
        {
            query = query.Where(t => t.SubmittedByUserId == userId);
        }

        var ticket = await query
            .Include(t => t.DepartmentEntity)
            .Include(t => t.Location)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(cancellationToken);

        if (ticket == null)
        {
            return null;
        }

        return new MaintenanceRequestDetailsPageViewModel
        {
            Request = new MaintenanceRequestDetailsDto
            {
                Id = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                DepartmentName = ticket.DepartmentEntity?.Name ?? ticket.Department,
                LocationName = ticket.Location?.Name ?? string.Empty,
                Building = ticket.Location?.Building,
                Floor = ticket.Location?.Floor,
                RequiredSkill = ticket.RequiredSkill,
                ImpactScope = ticket.ImpactScope?.ToString() ?? string.Empty,
                HazardLevel = ticket.HazardLevel?.ToString() ?? string.Empty,
                LocationCriticality = ticket.LocationCriticality?.ToString() ?? string.Empty,
                Priority = ticket.Priority.ToString(),
                PriorityScore = ticket.PriorityScore,
                Status = ticket.Status.ToString(),
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                ResolvedAt = ticket.ResolvedAt
            },
            Attachments = ticket.Attachments.ToList()
        };
    }

    public async Task<B2BDashboardViewModel> GetDashboardAsync(
        string userId,
        bool canViewAll,
        CancellationToken cancellationToken = default)
    {
        var query = _context.MaintenanceTickets.AsNoTracking().AsQueryable();
        if (!canViewAll)
        {
            query = query.Where(t => t.SubmittedByUserId == userId);
        }

        var recentRequests = await query
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(ListDtoProjection)
            .ToListAsync(cancellationToken);

        return new B2BDashboardViewModel
        {
            TotalRequests = await query.CountAsync(cancellationToken),
            PendingRequests = await query.CountAsync(t => t.Status == TicketStatus.Pending, cancellationToken),
            AssignedRequests = await query.CountAsync(t => t.Status == TicketStatus.Assigned, cancellationToken),
            InProgressRequests = await query.CountAsync(t => t.Status == TicketStatus.InProgress, cancellationToken),
            ResolvedRequests = await query.CountAsync(t => t.Status == TicketStatus.Resolved || t.Status == TicketStatus.Closed, cancellationToken),
            HighPriorityRequests = await query.CountAsync(t => t.Priority == TicketPriority.High, cancellationToken),
            CriticalPriorityRequests = await query.CountAsync(t => t.Priority == TicketPriority.Critical, cancellationToken),
            RecentRequests = recentRequests
        };
    }

    private static readonly Expression<Func<MaintenanceTicket, MaintenanceRequestListDto>> ListDtoProjection =
        ticket => new MaintenanceRequestListDto
        {
            Id = ticket.Id,
            Title = ticket.Title,
            DepartmentName = ticket.DepartmentEntity != null ? ticket.DepartmentEntity.Name : ticket.Department,
            LocationName = ticket.Location != null ? ticket.Location.Name : string.Empty,
            Building = ticket.Location != null ? ticket.Location.Building : null,
            Floor = ticket.Location != null ? ticket.Location.Floor : null,
            Priority = ticket.Priority.ToString(),
            PriorityScore = ticket.PriorityScore,
            Status = ticket.Status.ToString(),
            CreatedAt = ticket.CreatedAt
        };
}
