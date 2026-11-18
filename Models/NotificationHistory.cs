namespace Fixawyy.Models;

public class NotificationHistory
{
    public int Id { get; set; }

    public string? RecipientUserId { get; set; }
    public string TargetRole { get; set; } = "FacilityAdmin"; // FacilityAdmin, Technician, All

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // نوع الحدث: NewAssignment, StatusChanged, NewNotification
    public string EventType { get; set; } = "NewNotification";

    public int? RelatedTicketId { get; set; }
    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
