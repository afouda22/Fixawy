using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartMaintenance.Dispatcher.Data;
using SmartMaintenance.Dispatcher.DTOs;
using SmartMaintenance.Dispatcher.Hubs;
using SmartMaintenance.Dispatcher.Models;

namespace SmartMaintenance.Dispatcher.Services;

public class TechnicianAssignmentService : ITechnicianAssignmentService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public TechnicianAssignmentService(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<List<TicketSummaryDto>> GetPendingTicketsAsync()
    {
        return await _context.Tickets
            .AsNoTracking()
            .Where(t => t.Status == TicketStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new TicketSummaryDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Department = t.Department,
                RequiredSkill = t.RequiredSkill,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<TechnicianMatchResultDto>> GetMatchingTechniciansAsync(int ticketId)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket with ID {ticketId} not found.");
        }

        // 1. الفلتر الإجباري (Hard Filter): فني متاح + تخصص يطابق المهارة المطلوبة
        var candidates = await _context.Technicians
            .Where(t => t.IsAvailable && t.Specialization.ToLower() == ticket.RequiredSkill.ToLower())
            .ToListAsync();

        if (!candidates.Any())
        {
            return new List<TechnicianMatchResultDto>();
        }

        var results = new List<TechnicianMatchResultDto>();

        foreach (var tech in candidates)
        {
            var reasons = new List<string>();

            // 1. مهارة مطابقة (40 نقطة)
            double skillScore = 40.0;
            reasons.Add($"Matching specialization: {tech.Specialization} (+40 pts)");

            // 2. توزيع ضغط العمل (Workload Balance - حتى 35 نقطة)
            double workloadScore = Math.Max(0.0, (5 - tech.ActiveTicketsCount) * 7.0);
            reasons.Add($"Workload capacity: {tech.ActiveTicketsCount} active jobs (+{workloadScore:0.0} pts)");

            // 3. تقييم الفني (Rating - حتى 25 نقطة)
            double ratingScore = Math.Round((tech.Rating / 5.0) * 25.0, 1);
            reasons.Add($"Customer rating: {tech.Rating:0.0}/5.0 (+{ratingScore:0.0} pts)");

            // المجموع الكلي من 100
            double totalScore = Math.Round(skillScore + workloadScore + ratingScore, 1);

            results.Add(new TechnicianMatchResultDto
            {
                TechnicianId = tech.Id,
                FullName = tech.FullName,
                Specialization = tech.Specialization,
                Rating = tech.Rating,
                ActiveTicketsCount = tech.ActiveTicketsCount,
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
        var ticket = await _context.Tickets.FindAsync(ticketId);
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
            throw new InvalidOperationException($"No available technicians found with specialization: {ticket.RequiredSkill}");
        }

        var bestMatch = candidates.First();
        var technician = await _context.Technicians.FindAsync(bestMatch.TechnicianId);
        if (technician == null)
        {
            throw new InvalidOperationException("Selected technician could not be found.");
        }

        // 1. إنشاء سجل التعيين
        var assignment = new Assignment
        {
            TicketId = ticket.Id,
            TechnicianId = technician.Id,
            AssignedAt = DateTime.UtcNow,
            Status = "Active",
            MatchScore = bestMatch.MatchScore,
            Notes = notes ?? $"Auto-assigned by Smart Dispatcher Engine (Score: {bestMatch.MatchScore}%)"
        };

        _context.Assignments.Add(assignment);

        // 2. تحديث حالة التذكرة
        ticket.Status = TicketStatus.Assigned;

        // 3. زيادة عدد المهام النشطة للفني
        technician.ActiveTicketsCount += 1;

        // 4. تسجيل إشعار في سجل الإشعارات
        var notification = new NotificationHistory
        {
            TargetRole = "Technician",
            RecipientUserId = technician.Id,
            Title = "New Ticket Assigned",
            Message = $"You have been assigned ticket: '{ticket.Title}' ({ticket.Priority} Priority).",
            EventType = "NewAssignment",
            RelatedTicketId = ticket.Id,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
        _context.NotificationHistories.Add(notification);

        await _context.SaveChangesAsync();

        // 5. إطلاق أحداث الـ SignalR الحية (Real-Time Push Events)
        await _hubContext.Clients.All.SendAsync("NewAssignment", new
        {
            assignmentId = assignment.Id,
            ticketId = ticket.Id,
            ticketTitle = ticket.Title,
            technicianId = technician.Id,
            technicianName = technician.FullName,
            assignedAt = assignment.AssignedAt,
            matchScore = assignment.MatchScore
        });

        await _hubContext.Clients.All.SendAsync("StatusChanged", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
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
            TechnicianName = technician.FullName,
            AssignedAt = assignment.AssignedAt,
            MatchScore = assignment.MatchScore,
            Status = assignment.Status,
            Notes = assignment.Notes
        };
    }

    public async Task<AssignmentResponseDto> ManualAssignTechnicianAsync(int ticketId, int technicianId, string? notes = null)
    {
        var ticket = await _context.Tickets.FindAsync(ticketId);
        if (ticket == null)
        {
            throw new ArgumentException($"Ticket with ID {ticketId} not found.");
        }

        var technician = await _context.Technicians.FindAsync(technicianId);
        if (technician == null)
        {
            throw new ArgumentException($"Technician with ID {technicianId} not found.");
        }

        var suggestions = await GetMatchingTechniciansAsync(ticketId);
        var match = suggestions.FirstOrDefault(s => s.TechnicianId == technicianId);
        double score = match?.MatchScore ?? 70.0;

        var assignment = new Assignment
        {
            TicketId = ticket.Id,
            TechnicianId = technician.Id,
            AssignedAt = DateTime.UtcNow,
            Status = "Active",
            MatchScore = score,
            Notes = notes ?? "Manually assigned by Operations Manager"
        };

        _context.Assignments.Add(assignment);
        ticket.Status = TicketStatus.Assigned;
        technician.ActiveTicketsCount += 1;

        var notification = new NotificationHistory
        {
            TargetRole = "Technician",
            RecipientUserId = technician.Id,
            Title = "New Ticket Assigned",
            Message = $"You were manually assigned ticket: '{ticket.Title}' by Manager.",
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
            technicianId = technician.Id,
            technicianName = technician.FullName,
            assignedAt = assignment.AssignedAt,
            matchScore = assignment.MatchScore
        });

        await _hubContext.Clients.All.SendAsync("StatusChanged", new
        {
            ticketId = ticket.Id,
            title = ticket.Title,
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
            TechnicianName = technician.FullName,
            AssignedAt = assignment.AssignedAt,
            MatchScore = assignment.MatchScore,
            Status = assignment.Status,
            Notes = assignment.Notes
        };
    }

    public async Task<List<AssignmentResponseDto>> GetActiveAssignmentsAsync()
    {
        return await _context.Assignments
            .AsNoTracking()
            .Include(a => a.Ticket)
            .Include(a => a.Technician)
            .Where(a => a.Status == "Active")
            .OrderByDescending(a => a.AssignedAt)
            .Select(a => new AssignmentResponseDto
            {
                AssignmentId = a.Id,
                TicketId = a.TicketId,
                TicketTitle = a.Ticket != null ? a.Ticket.Title : string.Empty,
                TechnicianId = a.TechnicianId,
                TechnicianName = a.Technician != null ? a.Technician.FullName : string.Empty,
                AssignedAt = a.AssignedAt,
                MatchScore = a.MatchScore,
                Status = a.Status,
                Notes = a.Notes
            })
            .ToListAsync();
    }

    public async Task<bool> ResolveAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Ticket)
            .Include(a => a.Technician)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);

        if (assignment == null) return false;

        assignment.Status = "Completed";
        assignment.CompletedAt = DateTime.UtcNow;

        if (assignment.Ticket != null)
        {
            assignment.Ticket.Status = TicketStatus.Resolved;
            assignment.Ticket.ResolvedAt = DateTime.UtcNow;
        }

        if (assignment.Technician != null && assignment.Technician.ActiveTicketsCount > 0)
        {
            assignment.Technician.ActiveTicketsCount -= 1;
        }

        var notification = new NotificationHistory
        {
            TargetRole = "All",
            Title = "Ticket Resolved",
            Message = $"Ticket '{assignment.Ticket?.Title}' has been resolved by {assignment.Technician?.FullName}.",
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
}
