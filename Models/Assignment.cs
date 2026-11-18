namespace Fixawyy.Models;

public class Assignment
{
    public int Id { get; set; }

    public int TicketId { get; set; }
    public virtual MaintenanceTicket? Ticket { get; set; }

    public int TechnicianProfileId { get; set; }
    public virtual TechnicianProfile? Technician { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    // الحالة: Active, Completed, Reassigned, Cancelled
    public string Status { get; set; } = "Active";

    // النقاط اللي الخوارزمية اختارت الفني بناءً عليها (0 - 100)
    public double MatchScore { get; set; }

    // ملاحظات التعيين (سواء تم تلقائياً عبر الخوارزمية أو يدوياً عبر المدير)
    public string? Notes { get; set; }
}
