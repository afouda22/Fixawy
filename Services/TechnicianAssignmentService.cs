using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Hubs;
using Fixawyy.Models;

namespace Fixawyy.Services;

public class TechnicianAssignmentService : ITechnicianAssignmentService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public TechnicianAssignmentService(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<List<TicketSummaryDto>> GetPendingTicketsAsync(string? facility = null)
    {
        var allPending = await _context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.Status == TicketStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(facility) && !facility.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var fac = facility.Trim().ToLower();
            allPending = allPending.Where(t =>
                !string.IsNullOrEmpty(t.Department) && (
                    t.Department.ToLower().Contains(fac) ||
                    fac.Contains(t.Department.ToLower()) ||
                    (fac.Contains("engineer") && (t.Department.ToLower().Contains("engineer") || t.Department.ToLower().Contains("it") || t.Department.ToLower().Contains("building a") || t.Department.ToLower().Contains("stairwell") || t.Department.ToLower().Contains("lab") || t.Department.ToLower().Contains("faculty"))) ||
                    (fac.Contains("mall") && (t.Department.ToLower().Contains("mall") || t.Department.ToLower().Contains("cafeteria") || t.Department.ToLower().Contains("food") || t.Department.ToLower().Contains("front office") || t.Department.ToLower().Contains("suite") || t.Department.ToLower().Contains("lounge") || t.Department.ToLower().Contains("parking")))
                )
            ).ToList();
        }

        return allPending.Select(t => new TicketSummaryDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Department = t.Department,
            RequiredSkill = t.RequiredSkill,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public static bool IsSpecialtyMatch(string? requiredSkill, IEnumerable<string> technicianSpecialties)
    {
        if (string.IsNullOrWhiteSpace(requiredSkill)) return true;
        var req = requiredSkill.Trim().ToLower();

        return technicianSpecialties.Any(spec =>
        {
            if (string.IsNullOrWhiteSpace(spec)) return false;
            var s = spec.Trim().ToLower();

            if (s.Contains(req) || req.Contains(s)) return true;

            // AC / HVAC keywords
            bool reqIsHvac = req.Contains("ac") || req.Contains("air") || req.Contains("hvac") || req.Contains("cool") || req.Contains("heat");
            bool specIsHvac = s.Contains("ac") || s.Contains("air") || s.Contains("hvac") || s.Contains("cool") || s.Contains("heat");
            if (reqIsHvac && specIsHvac) return true;

            // Electric / Power keywords
            bool reqIsElec = req.Contains("electr") || req.Contains("power") || req.Contains("circuit") || req.Contains("wire") || req.Contains("lighting");
            bool specIsElec = s.Contains("electr") || s.Contains("power") || s.Contains("circuit") || s.Contains("wire") || s.Contains("lighting");
            if (reqIsElec && specIsElec) return true;

            // Plumbing / Pipe / Water keywords
            bool reqIsPlumb = req.Contains("plumb") || req.Contains("pipe") || req.Contains("leak") || req.Contains("drain") || req.Contains("water") || req.Contains("sanitary");
            bool specIsPlumb = s.Contains("plumb") || s.Contains("pipe") || s.Contains("leak") || s.Contains("drain") || s.Contains("water") || s.Contains("sanitary");
            if (reqIsPlumb && specIsPlumb) return true;

            // Carpentry / Wood / Lock keywords
            bool reqIsCarp = req.Contains("carpenter") || req.Contains("wood") || req.Contains("furniture") || req.Contains("door") || req.Contains("lock") || req.Contains("cabinet");
            bool specIsCarp = s.Contains("carpenter") || s.Contains("wood") || s.Contains("furniture") || s.Contains("door") || s.Contains("lock") || s.Contains("cabinet");
            if (reqIsCarp && specIsCarp) return true;

            // Painting / Wall keywords
            bool reqIsPaint = req.Contains("paint") || req.Contains("wall") || req.Contains("decor");
            bool specIsPaint = s.Contains("paint") || s.Contains("wall") || s.Contains("decor");
            if (reqIsPaint && specIsPaint) return true;

            // Appliance Repair keywords
            bool reqIsApp = req.Contains("appliance") || req.Contains("refrigerator") || req.Contains("washing") || req.Contains("oven");
            bool specIsApp = s.Contains("appliance") || s.Contains("refrigerator") || s.Contains("washing") || s.Contains("oven");
            if (reqIsApp && specIsApp) return true;

            return false;
        });
    }

    public async Task<List<TechnicianMatchResultDto>> GetMatchingTechniciansAsync(int ticketId)
    {
        var ticket = await _context.MaintenanceTickets.FindAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket with ID {ticketId} not found.");
        }

        var allTechnicians = await _context.TechnicianProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.TechnicianSpecialties)
                .ThenInclude(ts => ts.Specialty)
            .Include(tp => tp.Assignments)
            .Where(tp => tp.IsAvailable)
            .ToListAsync();

        if (!allTechnicians.Any())
        {
            return new List<TechnicianMatchResultDto>();
        }

        // مطابقة التخصص مع المهارة المطلوبة
        var candidates = allTechnicians.Where(tp =>
        {
            var specs = tp.TechnicianSpecialties
                .Where(ts => ts.Specialty != null)
                .Select(ts => ts.Specialty!.Name);
            return IsSpecialtyMatch(ticket.RequiredSkill, specs);
        }).ToList();

        bool hasSkillMatch = candidates.Any();
        var pool = hasSkillMatch ? candidates : allTechnicians;

        var results = new List<TechnicianMatchResultDto>();

        foreach (var tech in pool)
        {
            var reasons = new List<string>();
            var specNames = string.Join(", ", tech.TechnicianSpecialties
                .Where(ts => ts.Specialty != null)
                .Select(ts => ts.Specialty!.Name));

            if (string.IsNullOrEmpty(specNames)) specNames = "General Maintenance";

            // 1. التخصص (حتى 40 نقطة)
            double skillScore = hasSkillMatch ? 40.0 : 15.0;
            reasons.Add(hasSkillMatch
                ? $"Matching specialization: {specNames} (+40 pts)"
                : $"Alternative specialization: {specNames} (+15 pts)");

            // 2. توزيع ضغط العمل (Workload Balance - حتى 35 نقطة)
            int activeJobs = tech.Assignments.Count(a => a.Status == "Active");
            double workloadScore = Math.Max(0.0, (5 - activeJobs) * 7.0);
            reasons.Add($"Workload capacity: {activeJobs} active jobs (+{workloadScore:0.0} pts)");

            // 3. تقييم الفني (Rating - حتى 25 نقطة)
            double ratingScore = Math.Round((tech.Rating / 5.0) * 25.0, 1);
            reasons.Add($"Customer rating: {tech.Rating:0.0}/5.0 (+{ratingScore:0.0} pts)");

            // المجموع الكلي من 100
            double totalScore = Math.Round(skillScore + workloadScore + ratingScore, 1);

            results.Add(new TechnicianMatchResultDto
            {
                TechnicianId = tech.Id,
                FullName = tech.User?.FullName ?? $"Technician #{tech.Id}",
                Specialization = specNames,
                Rating = tech.Rating,
                ActiveTicketsCount = activeJobs,
                MatchScore = totalScore,
                MatchReasons = reasons
            });
        }

        var sorted = results.OrderByDescending(r => r.MatchScore).ToList();
        if (sorted.Count > 0)
        {
            sorted[0].IsRecommended = true;
        }

        return sorted;
    }

    public async Task<AssignmentResponseDto> AutoAssignTechnicianAsync(int ticketId, string? notes = null)
    {
        var ticket = await _context.MaintenanceTickets.FindAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket with ID {ticketId} not found.");
        }

        if (ticket.Status != TicketStatus.Pending)
        {
            throw new InvalidOperationException($"Ticket {ticketId} is already assigned or resolved.");
        }

        var candidates = await GetMatchingTechniciansAsync(ticketId);
        if (!candidates.Any())
        {
            throw new InvalidOperationException($"No available technicians found for skill: {ticket.RequiredSkill}");
        }

        var bestMatch = candidates.First();
        var technician = await _context.TechnicianProfiles
            .Include(tp => tp.User)
            .FirstOrDefaultAsync(tp => tp.Id == bestMatch.TechnicianId);

        if (technician == null)
        {
            throw new InvalidOperationException("Selected technician profile could not be found.");
        }

        // 1. إنشاء سجل التعيين
        var assignment = new Assignment
        {
            TicketId = ticket.Id,
            TechnicianProfileId = technician.Id,
            AssignedAt = DateTime.UtcNow,
            Status = "Active",
            MatchScore = bestMatch.MatchScore,
            Notes = notes ?? $"Auto-assigned by Facility Dispatcher Engine (Score: {bestMatch.MatchScore}%)"
        };

        _context.Assignments.Add(assignment);

        // 2. تحديث حالة التذكرة
        ticket.Status = TicketStatus.Assigned;

        // 3. تسجيل إشعار في سجل الإشعارات
        var techName = technician.User?.FullName ?? $"Technician #{technician.Id}";
        var notification = new NotificationHistory
        {
            TargetRole = "Technician",
            RecipientUserId = technician.UserId,
            Title = "New Ticket Assigned",
            Message = $"You have been assigned ticket: '{ticket.Title}' ({ticket.Priority} Priority) in {ticket.Department}.",
            EventType = "NewAssignment",
            RelatedTicketId = ticket.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.NotificationHistories.Add(notification);

        await _context.SaveChangesAsync();

        // 4. إطلاق أحداث الـ SignalR الحية (Real-Time Push Events)
        await _hubContext.Clients.All.SendAsync("NewAssignment", new
        {
            assignmentId = assignment.Id,
            ticketId = ticket.Id,
            ticketTitle = ticket.Title,
            department = ticket.Department,
            technicianId = technician.Id,
            technicianName = techName,
            assignedAt = assignment.AssignedAt,
            matchScore = assignment.MatchScore
        });

        await _hubContext.Clients.All.SendAsync("StatusChanged", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            department = ticket.Department,
            newStatus = "Assigned",
            updatedAt = DateTime.UtcNow
        });

        await _hubContext.Clients.All.SendAsync("NewNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            targetRole = notification.TargetRole,
            eventType = notification.EventType,
            createdAt = notification.CreatedAt
        });

        return new AssignmentResponseDto
        {
            AssignmentId = assignment.Id,
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            TechnicianId = technician.Id,
            TechnicianName = techName,
            AssignedAt = assignment.AssignedAt,
            MatchScore = assignment.MatchScore,
            Status = assignment.Status,
            Notes = assignment.Notes
        };
    }

    public async Task<AssignmentResponseDto> ManualAssignTechnicianAsync(int ticketId, int technicianId, string? notes = null)
    {
        var ticket = await _context.MaintenanceTickets.FindAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket with ID {ticketId} not found.");
        }

        if (ticket.Status != TicketStatus.Pending)
        {
            throw new InvalidOperationException($"Ticket #{ticketId} is already {ticket.Status} and cannot be assigned again.");
        }

        var technician = await _context.TechnicianProfiles
            .Include(tp => tp.User)
            .Include(tp => tp.TechnicianSpecialties)
                .ThenInclude(ts => ts.Specialty)
            .FirstOrDefaultAsync(tp => tp.Id == technicianId);

        if (technician == null)
        {
            throw new ArgumentException($"Technician Profile with ID {technicianId} not found.");
        }

        var techName = technician.User?.FullName ?? $"Technician #{technician.Id}";

        if (!technician.IsAvailable)
        {
            throw new InvalidOperationException($"Technician '{techName}' is currently marked as unavailable.");
        }

        // Validate Skill Match between Ticket and Technician
        var techSpecs = technician.TechnicianSpecialties
            .Where(ts => ts.Specialty != null)
            .Select(ts => ts.Specialty!.Name)
            .ToList();

        if (!IsSpecialtyMatch(ticket.RequiredSkill, techSpecs))
        {
            var specsStr = techSpecs.Any() ? string.Join(", ", techSpecs) : "General Maintenance";
            throw new InvalidOperationException($"Skill Mismatch Error: Ticket #{ticket.Id} requires '{ticket.RequiredSkill}', but technician '{techName}' specializes in '{specsStr}'. Drag & Drop assignment rejected.");
        }

        var suggestions = await GetMatchingTechniciansAsync(ticketId);
        var match = suggestions.FirstOrDefault(s => s.TechnicianId == technicianId);
        double score = match?.MatchScore ?? 85.0;

        var assignment = new Assignment
        {
            TicketId = ticket.Id,
            TechnicianProfileId = technician.Id,
            AssignedAt = DateTime.UtcNow,
            Status = "Active",
            MatchScore = score,
            Notes = notes ?? "Manually assigned by Facility Admin"
        };

        _context.Assignments.Add(assignment);
        ticket.Status = TicketStatus.Assigned;

        var notification = new NotificationHistory
        {
            TargetRole = "Technician",
            RecipientUserId = technician.UserId,
            Title = "New Ticket Assigned",
            Message = $"You were manually assigned ticket: '{ticket.Title}' ({ticket.Department}) by Facility Admin.",
            EventType = "NewAssignment",
            RelatedTicketId = ticket.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.NotificationHistories.Add(notification);

        await _context.SaveChangesAsync();

        // إطلاق أحداث SignalR
        await _hubContext.Clients.All.SendAsync("NewAssignment", new
        {
            assignmentId = assignment.Id,
            ticketId = ticket.Id,
            ticketTitle = ticket.Title,
            department = ticket.Department,
            technicianId = technician.Id,
            technicianName = techName,
            assignedAt = assignment.AssignedAt,
            matchScore = assignment.MatchScore
        });

        await _hubContext.Clients.All.SendAsync("StatusChanged", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
            department = ticket.Department,
            newStatus = "Assigned",
            updatedAt = DateTime.UtcNow
        });

        await _hubContext.Clients.All.SendAsync("NewNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            targetRole = notification.TargetRole,
            eventType = notification.EventType,
            createdAt = notification.CreatedAt
        });

        return new AssignmentResponseDto
        {
            AssignmentId = assignment.Id,
            TicketId = ticket.Id,
            TicketTitle = ticket.Title,
            TechnicianId = technician.Id,
            TechnicianName = techName,
            AssignedAt = assignment.AssignedAt,
            MatchScore = assignment.MatchScore,
            Status = assignment.Status,
            Notes = assignment.Notes
        };
    }

    public async Task<List<AssignmentResponseDto>> GetActiveAssignmentsAsync(string? facility = null)
    {
        var allActive = await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Ticket)
            .Include(a => a.Technician)
                .ThenInclude(tp => tp!.User)
            .Where(a => a.Status == "Active")
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(facility) && !facility.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var fac = facility.Trim().ToLower();
            allActive = allActive.Where(a =>
                a.Ticket != null && !string.IsNullOrEmpty(a.Ticket.Department) && (
                    a.Ticket.Department.ToLower().Contains(fac) ||
                    fac.Contains(a.Ticket.Department.ToLower()) ||
                    (fac.Contains("engineer") && (a.Ticket.Department.ToLower().Contains("engineer") || a.Ticket.Department.ToLower().Contains("utility") || a.Ticket.Department.ToLower().Contains("tower") || a.Ticket.Department.ToLower().Contains("medical") || a.Ticket.Department.ToLower().Contains("chiller"))) ||
                    (fac.Contains("mall") && (a.Ticket.Department.ToLower().Contains("mall") || a.Ticket.Department.ToLower().Contains("parking") || a.Ticket.Department.ToLower().Contains("lobby") || a.Ticket.Department.ToLower().Contains("cafeteria")))
                )
            ).ToList();
        }

        return allActive.Select(a => new AssignmentResponseDto
        {
            AssignmentId = a.Id,
            TicketId = a.TicketId,
            TicketTitle = a.Ticket != null ? a.Ticket.Title : string.Empty,
            TechnicianId = a.TechnicianProfileId,
            TechnicianName = a.Technician != null && a.Technician.User != null ? a.Technician.User.FullName : $"Tech #{a.TechnicianProfileId}",
            AssignedAt = a.AssignedAt,
            MatchScore = a.MatchScore,
            Status = a.Status,
            Notes = a.Notes
        }).ToList();
    }

    public async Task<bool> ResolveAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Ticket)
            .Include(a => a.Technician)
                .ThenInclude(tp => tp!.User)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null) return false;

        assignment.Status = "Completed";
        assignment.CompletedAt = DateTime.UtcNow;

        if (assignment.Ticket != null)
        {
            assignment.Ticket.Status = TicketStatus.Resolved;
            assignment.Ticket.ResolvedAt = DateTime.UtcNow;
        }

        var techName = assignment.Technician?.User?.FullName ?? $"Tech #{assignment.TechnicianProfileId}";

        var notification = new NotificationHistory
        {
            TargetRole = "All",
            Title = "Ticket Resolved",
            Message = $"Ticket '{assignment.Ticket?.Title}' has been resolved by {techName}.",
            EventType = "StatusChanged",
            RelatedTicketId = assignment.TicketId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.NotificationHistories.Add(notification);

        await _context.SaveChangesAsync();

        // بث SignalR حدث StatusChanged
        await _hubContext.Clients.All.SendAsync("StatusChanged", new
        {
            ticketId = assignment.TicketId,
            title = assignment.Ticket?.Title,
            department = assignment.Ticket?.Department,
            newStatus = "Resolved",
            updatedAt = DateTime.UtcNow
        });

        await _hubContext.Clients.All.SendAsync("NewNotification", new
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            targetRole = notification.TargetRole,
            eventType = notification.EventType,
            createdAt = notification.CreatedAt
        });

        return true;
    }

    public async Task<List<string>> GetFacilitiesAsync()
    {
        return await Task.FromResult(AppFacilities.All.ToList());
    }
}
