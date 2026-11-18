namespace Fixawyy.Models;

public class MaintenanceTicket
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // القسم أو المكان (مثلاً: Building A, Mall Food Court, Faculty of Engineering)
    public string Department { get; set; } = string.Empty;

    // المهارة المطلوبة لإصلاح العطل
    public string RequiredSkill { get; set; } = string.Empty;

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public TicketStatus Status { get; set; } = TicketStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    // علاقة التذكرة بعمليات التعيين
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
