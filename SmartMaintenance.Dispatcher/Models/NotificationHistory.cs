namespace SmartMaintenance.Dispatcher.Models;

public class NotificationHistory
{
    public int Id { get; set; }

    public int? RecipientUserId { get; set; }
    public string TargetRole { get; set; } = "Manager"; // Manager, Technician, All

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    // نوع الحدث: NewAssignment, StatusChanged, NewNotification
    public string EventType { get; set; } = "NewNotification";

    public int? RelatedTicketId { get; set; }
    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}