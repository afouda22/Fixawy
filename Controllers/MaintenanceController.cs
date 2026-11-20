using Fixawyy.DTOs;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.FacilityAdmin},{AppRoles.Employee}")]
public class MaintenanceController : Controller
{
    private readonly IMaintenanceRequestService _requestService;
    private readonly IDepartmentService _departmentService;
    private readonly ILocationService _locationService;
    private readonly IAttachmentService _attachmentService;
    private readonly ICurrentActorProvider _actors;

    public MaintenanceController(
        IMaintenanceRequestService requestService,
        IDepartmentService departmentService,
        ILocationService locationService,
        IAttachmentService attachmentService,
        ICurrentActorProvider actors)
    {
        _requestService = requestService;
        _departmentService = departmentService;
        _locationService = locationService;
        _attachmentService = attachmentService;
        _actors = actors;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        var model = await _requestService.GetDashboardAsync(
            actor.UserId,
            User.IsInRole(AppRoles.Admin),
            cancellationToken);
        ViewBag.CanViewAll = User.IsInRole(AppRoles.Admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        MaintenanceRequestFilterViewModel filter,
        CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        var model = await _requestService.GetRequestsAsync(
            actor.UserId,
            User.IsInRole(AppRoles.Admin),
            filter,
            cancellationToken);

        model.Filter.Departments = await _departmentService.GetAllAsync(cancellationToken);
        model.Filter.Locations = await _locationService.GetAllAsync(cancellationToken);
        ViewBag.CanViewAll = User.IsInRole(AppRoles.Admin);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        return View(await BuildCreateModelAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateMaintenanceRequestViewModel model,
        CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCreateModelAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            var requestId = await _requestService.CreateRequestAsync(
                new CreateMaintenanceRequestDto
                {
                    Title = model.Title,
                    Description = model.Description,
                    DepartmentId = model.DepartmentId,
                    LocationId = model.LocationId,
                    RequiredSkill = model.RequiredSkill,
                    ImpactScope = model.ImpactScope,
                    HazardLevel = model.HazardLevel,
                    LocationCriticality = model.LocationCriticality
                },
                actor.UserId);

            var attachmentsFailed = false;
            if (model.Attachments.Count > 0)
            {
                try
                {
                    await _attachmentService.UploadAsync(
                        requestId,
                        actor.UserId,
                        User.IsInRole(AppRoles.Admin),
                        model.Attachments,
                        cancellationToken);
                }
                catch (InvalidDataException ex)
                {
                    TempData["ErrorMessage"] = $"Request created, but attachments were not uploaded: {ex.Message}";
                    attachmentsFailed = true;
                }
            }

            if (!attachmentsFailed)
            {
                TempData["SuccessMessage"] = "Maintenance request created successfully.";
            }
            return RedirectToAction(nameof(Details), new { id = requestId });
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            await PopulateCreateModelAsync(model, cancellationToken);
            return View(model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        var model = await _requestService.GetDetailsForUserAsync(
            id,
            actor.UserId,
            User.IsInRole(AppRoles.Admin),
            cancellationToken);

        if (model != null)
        {
            model.CanManageAttachments = true;
        }

        return model == null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadAttachment(int attachmentId, CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        var download = await _attachmentService.DownloadAsync(
            attachmentId,
            actor.UserId,
            User.IsInRole(AppRoles.Admin),
            cancellationToken);

        return download == null
            ? NotFound()
            : File(download.Content, download.ContentType, download.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAttachment(int attachmentId, CancellationToken cancellationToken)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null)
        {
            return Challenge();
        }

        var result = await _attachmentService.DeleteAsync(
            attachmentId,
            actor.UserId,
            User.IsInRole(AppRoles.Admin),
            cancellationToken);

        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] =
            result.Succeeded ? "Attachment deleted successfully." : result.Error;
        return RedirectToAction(nameof(Details), new { id = Request.Query["ticketId"].ToString() });
    }

    private async Task<CreateMaintenanceRequestViewModel> BuildCreateModelAsync(
        CancellationToken cancellationToken)
    {
        var model = new CreateMaintenanceRequestViewModel();
        await PopulateCreateModelAsync(model, cancellationToken);
        return model;
    }

    private async Task PopulateCreateModelAsync(
        CreateMaintenanceRequestViewModel model,
        CancellationToken cancellationToken)
    {
        model.Departments = await _departmentService.GetAllAsync(cancellationToken);
        model.Locations = await _locationService.GetAllAsync(cancellationToken);
    }
}
