using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Models;
using Fixawyy.Services;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.OperationsAuthorizedRoles)]
[ApiController]
[Route("api/assignment")]
public class AssignmentApiController : ControllerBase
{
    private readonly ITechnicianAssignmentService _assignmentService;
    private readonly AppDbContext _context;

    public AssignmentApiController(ITechnicianAssignmentService assignmentService, AppDbContext context)
    {
        _assignmentService = assignmentService;
        _context = context;
    }

    [HttpGet("pending-tickets")]
    public async Task<ActionResult<List<TicketSummaryDto>>> GetPendingTickets()
    {
        var tickets = await _assignmentService.GetPendingTicketsAsync();
        return Ok(tickets);
    }

    [HttpGet("suggestions/{ticketId:int}")]
    public async Task<ActionResult<List<TechnicianMatchResultDto>>> GetSuggestions(int ticketId)
    {
        try
        {
            var results = await _assignmentService.GetMatchingTechniciansAsync(ticketId);
            return Ok(results);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("auto-assign/{ticketId:int}")]
    public async Task<ActionResult<AssignmentResponseDto>> AutoAssign(int ticketId, [FromBody] AssignmentRequestDto? request)
    {
        try
        {
            var result = await _assignmentService.AutoAssignTechnicianAsync(ticketId, request?.Notes);
            return Ok(result);
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

    [HttpPost("manual-assign")]
    public async Task<ActionResult<AssignmentResponseDto>> ManualAssign([FromBody] ManualAssignmentRequestDto request)
    {
        try
        {
            var result = await _assignmentService.ManualAssignTechnicianAsync(request.TicketId, request.TechnicianId, request.Notes);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<List<AssignmentResponseDto>>> GetActiveAssignments()
    {
        var list = await _assignmentService.GetActiveAssignmentsAsync();
        return Ok(list);
    }

    [HttpPost("resolve/{assignmentId:int}")]
    [HttpPut("{assignmentId:int}/resolve")]
    public async Task<IActionResult> ResolveAssignment(int assignmentId)
    {
        var success = await _assignmentService.ResolveAssignmentAsync(assignmentId);
        if (!success) return NotFound(new { message = $"Assignment {assignmentId} not found." });
        return Ok(new { message = "Assignment marked as completed and ticket resolved." });
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> GetRecentNotifications([FromQuery] int limit = 15)
    {
        var notifications = await _context.NotificationHistories
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
        return Ok(notifications);
    }
}
