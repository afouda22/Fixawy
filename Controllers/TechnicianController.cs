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

    public TechnicianController(ITechnicianService technicianService)
    {
        _technicianService = technicianService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Directory(int? specialtyId, string? search)
    {
        var model = await _technicianService.GetDirectoryAsync(specialtyId, search);
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
