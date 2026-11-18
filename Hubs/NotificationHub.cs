using Microsoft.AspNetCore.SignalR;

namespace Fixawyy.Hubs;

public class NotificationHub : Hub
{
    // عند اتصال عميل جديد
    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("Connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    // إرسال إشعار عام لجميع المتصلين (مديرين وفنيين)
    public async Task SendNotificationToAll(string title, string message, string eventType)
    {
        await Clients.All.SendAsync("NewNotification", new
        {
            title,
            message,
            eventType,
            createdAt = DateTime.UtcNow
        });
    }

    // إرسال إشعار مخصص لدور معين (مثل FacilityAdmin أو Technician)
    public async Task SendNotificationToRole(string role, string title, string message, string eventType)
    {
        await Clients.Group(role).SendAsync("NewNotification", new
        {
            title,
            message,
            eventType,
            targetRole = role,
            createdAt = DateTime.UtcNow
        });
    }

    // انضمام العميل لمجموعة بحسب دوره
    public async Task JoinRoleGroup(string role)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, role);
    }
}
