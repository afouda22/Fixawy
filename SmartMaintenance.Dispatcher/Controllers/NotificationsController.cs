using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartMaintenance.Dispatcher.Data;

namespace SmartMaintenance.Dispatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotificationsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var list = await _context.NotificationHistories
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .ToListAsync();

        var unreadCount = await _context.NotificationHistories.CountAsync(n => !n.IsRead);

        return Ok(new { notifications = list, unreadCount });
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var item = await _context.NotificationHistories.FindAsync(id);
        if (item == null) return NotFound();

        item.IsRead = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Marked as read" });
    }

    [HttpPut("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _context.NotificationHistories.Where(n => !n.IsRead).ToListAsync();
        foreach (var u in unread)
        {
            u.IsRead = true;
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = "All marked as read" });
    }
}
