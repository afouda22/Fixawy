using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Fixawyy.Context;
using Fixawyy.DTOs;
using Fixawyy.Models;
using Fixawyy.Services;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.DispatcherAuthorizedRoles)]
[ApiController]
[Route("api/assignment")]
public class AssignmentApiController : ControllerBase
{
    private readonly ITechnicianAssignmentService _assignmentService;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public AssignmentApiController(
        ITechnicianAssignmentService assignmentService,
        AppDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _assignmentService = assignmentService;
        _context = context;
        _userManager = userManager;
    }

    private async Task<string?> GetEffectiveFacilityAsync(string? requestedFacility)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return requestedFacility;

        bool isSuperAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
        if (isSuperAdmin)
        {
            return requestedFacility;
        }

        // Facility Admin is strictly locked to their assigned facility
        return !string.IsNullOrWhiteSpace(user.Facility) ? user.Facility : requestedFacility;
    }

    [HttpGet("pending-tickets")]
    public async Task<ActionResult<List<TicketSummaryDto>>> GetPendingTickets([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var tickets = await _assignmentService.GetPendingTicketsAsync(effectiveFacility);
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
    public async Task<ActionResult<List<AssignmentResponseDto>>> GetActiveAssignments([FromQuery] string? facility = null)
    {
        var effectiveFacility = await GetEffectiveFacilityAsync(facility);
        var list = await _assignmentService.GetActiveAssignmentsAsync(effectiveFacility);
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

    [HttpGet("facilities")]
    public async Task<ActionResult<List<string>>> GetFacilities()
    {
        var user = await _userManager.GetUserAsync(User);
        bool isSuperAdmin = user != null && await _userManager.IsInRoleAsync(user, AppRoles.Admin);

        if (!isSuperAdmin && !string.IsNullOrWhiteSpace(user?.Facility))
        {
            return Ok(new List<string> { user.Facility });
        }

        var facilities = await _assignmentService.GetFacilitiesAsync();
        return Ok(facilities);
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
