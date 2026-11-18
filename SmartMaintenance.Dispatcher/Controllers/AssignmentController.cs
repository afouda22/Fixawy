using Microsoft.AspNetCore.Mvc;
using SmartMaintenance.Dispatcher.DTOs;
using SmartMaintenance.Dispatcher.Services;

namespace SmartMaintenance.Dispatcher.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentController : ControllerBase
{
    private readonly ITechnicianAssignmentService _assignmentService;

    public AssignmentController(ITechnicianAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// استرجاع التذاكر المعلقة التي تنتظر التوزيع
    /// </summary>
    [HttpGet("pending-tickets")]
    public async Task<IActionResult> GetPendingTickets()
    {
        var tickets = await _assignmentService.GetPendingTicketsAsync();
        return Ok(tickets);
    }

    /// <summary>
    /// استدعاء خوارزمية التعيين الذكي لاقتراح أفضل الفنيين لتذكرة معينة
    /// </summary>
    [HttpGet("suggestions/{ticketId}")]
    public async Task<IActionResult> GetSuggestions(int ticketId)
    {
        try
        {
            var suggestions = await _assignmentService.GetMatchingTechniciansAsync(ticketId);
            return Ok(suggestions);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// التعيين التلقائي: الخوارزمية تختار أفضل فني وتسند له المهمة مباشرة
    /// </summary>
    [HttpPost("auto-assign/{ticketId}")]
    public async Task<IActionResult> AutoAssign(int ticketId, [FromBody] AutoAssignRequestDto? request = null)
    {
        try
        {
            var result = await _assignmentService.AutoAssignTechnicianAsync(ticketId, request?.Notes);
            return Ok(new { message = "Ticket successfully auto-assigned by smart engine", assignment = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// التعيين اليدوي بواسطة مدير الصيانة
    /// </summary>
    [HttpPost("manual-assign")]
    public async Task<IActionResult> ManualAssign([FromBody] ManualAssignRequestDto request)
    {
        try
        {
            var result = await _assignmentService.ManualAssignTechnicianAsync(request.TicketId, request.TechnicianId, request.Notes);
            return Ok(new { message = "Ticket successfully assigned manually", assignment = result });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// استرجاع كل التعيينات النشطة الحالية
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAssignments()
    {
        var assignments = await _assignmentService.GetActiveAssignmentsAsync();
        return Ok(assignments);
    }

    /// <summary>
    /// إنهاء المهمة وتحديد التذكرة كمحلولة (Resolved) وبث حدث StatusChanged عبر SignalR
    /// </summary>
    [HttpPut("{id}/resolve")]
    public async Task<IActionResult> ResolveAssignment(int id)
    {
        var success = await _assignmentService.ResolveAssignmentAsync(id);
        if (!success) return NotFound(new { message = "Assignment not found." });
        return Ok(new { message = "Assignment successfully completed and marked as Resolved." });
    }
}
