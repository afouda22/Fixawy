
namespace Fixawyy.Models;

public class MaintenanceTicket
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    // Keep existing property for compatibility with team code
    public string Department { get; set; } = string.Empty;

    public string RequiredSkill { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    // B2B Department
    public int? DepartmentId { get; set; }

    public virtual Department? DepartmentEntity { get; set; }

    // B2B Location
    public int? LocationId { get; set; }

    public virtual Location? Location { get; set; }

    // Smart Prioritization
    public ImpactScope? ImpactScope { get; set; }

    public HazardLevel? HazardLevel { get; set; }

    public LocationCriticality? LocationCriticality { get; set; }

    public int PriorityScore { get; set; }

    // Request submitter
    public string? SubmittedByUserId { get; set; }

    // Assignments relationship
    public virtual ICollection<Assignment> Assignments { get; set; }
        = new List<Assignment>();

    // Ticket attachments relationship
    public virtual ICollection<TicketAttachment> Attachments { get; set; }
        = new List<TicketAttachment>();
}
