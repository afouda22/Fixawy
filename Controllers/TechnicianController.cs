using System.Security.Claims;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

public class TechnicianController : Controller
{
    private readonly ITechnicianService _technicianService;
    private readonly ITechnicianCatalogService _catalogService;

    public TechnicianController(ITechnicianService technicianService, ITechnicianCatalogService catalogService)
    {
        _technicianService = technicianService;
        _catalogService = catalogService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Directory([FromQuery] TechnicianCatalogFilter filter)
    {
        // The directory has always listed every technician (available or busy) by default;
        // "available only" is applied only when the customer ticks it.
        if (!Request.Query.ContainsKey(nameof(TechnicianCatalogFilter.OnlyAvailable)))
            filter.OnlyAvailable = false;

        var model = await _catalogService.SearchAsync(filter);
        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Technician},{AppRoles.Admin}")]
    public async Task<IActionResult> Profile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var model = await _technicianService.GetProfileByUserIdAsync(userId);
        if (model == null) return NotFound();

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Technician},{AppRoles.Admin}")]
    public async Task<IActionResult> Profile(TechnicianProfileViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (!ModelState.IsValid) return View(model);
        var success = await _technicianService.UpdateProfileAsync(userId, model);
        if (success)
        {
            TempData["SuccessMessage"] = "Profile details updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not update profile.";
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize(Roles = $"{AppRoles.Technician},{AppRoles.Admin}")]
    public async Task<IActionResult> Schedule()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        var model = await _technicianService.GetScheduleByUserIdAsync(userId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{AppRoles.Technician},{AppRoles.Admin}")]
    public async Task<IActionResult> Schedule(TechnicianScheduleViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return Challenge();

        if (!ModelState.IsValid) return View(model);
        var success = await _technicianService.UpdateScheduleAsync(userId, model);
        if (success)
        {
            TempData["SuccessMessage"] = "Working schedule updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Could not update working schedule.";
        }

        return RedirectToAction(nameof(Schedule));
    }
}
