
using Fixawyy.DTOs;
using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.FacilityAdmin},{AppRoles.Employee}")]
[Route("api/[controller]")]
[ApiController]
public class MaintenanceRequestController : ControllerBase
{
    private readonly IMaintenanceRequestService _requestService;
    private readonly ICurrentActorProvider _actors;

    public MaintenanceRequestController(
        IMaintenanceRequestService requestService,
        ICurrentActorProvider actors)
    {
        _requestService = requestService;
        _actors = actors;
    }

    [HttpPost]
    public async Task<IActionResult> CreateRequest(
        [FromBody] CreateMaintenanceRequestDto request)
    {
        var actor = await _actors.GetCurrentAsync(User);

        if (actor == null)
            return Unauthorized();

        if (!actor.IsCustomer &&
            !User.IsInRole(AppRoles.Employee) &&
            !User.IsInRole(AppRoles.FacilityAdmin) &&
            !User.IsInRole(AppRoles.Admin))
        {
            return Forbid();
        }

        try
        {
            int requestId = await _requestService.CreateRequestAsync(
                request,
                actor.UserId);

            return CreatedAtAction(
                nameof(GetRequestById),
                new { id = requestId },
                new
                {
                    Id = requestId,
                    Message = "Maintenance request created successfully."
                });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests()
    {
        var actor = await _actors.GetCurrentAsync(User);

        if (actor == null)
            return Unauthorized();

        var requests = await _requestService.GetMyRequestsAsync(
            actor.UserId);

        return Ok(requests);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetRequestById(int id)
    {
        var actor = await _actors.GetCurrentAsync(User);

        if (actor == null)
            return Unauthorized();

        var request = await _requestService.GetRequestByIdAsync(
            id,
            actor.UserId);

        if (request == null)
            return NotFound();

        return Ok(request);
    }
}
