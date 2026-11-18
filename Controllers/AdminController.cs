using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : Controller
{
    private readonly IUserService _userService;

    public AdminController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<IActionResult> Users(string? role, string? search)
    {
        var model = await _userService.GetUsersAsync(role, search);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> AssignRole(string userId, string newRole, string? facility)
    {
        var success = await _userService.AssignRoleAsync(userId, newRole, facility);
        if (success)
        {
            var facInfo = !string.IsNullOrWhiteSpace(facility) ? $" (Facility: {facility})" : "";
            TempData["SuccessMessage"] = $"Role changed to '{newRole}'{facInfo} successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update role.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> ToggleStatus(string userId)
    {
        var success = await _userService.ToggleUserStatusAsync(userId);
        if (success)
        {
            TempData["SuccessMessage"] = "User status updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to toggle user status.";
        }
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var (success, message) = await _userService.DeleteUserAsync(userId);
        if (success)
        {
            TempData["SuccessMessage"] = message;
        }
        else
        {
            TempData["ErrorMessage"] = message;
        }
        return RedirectToAction(nameof(Users));
    }
}
