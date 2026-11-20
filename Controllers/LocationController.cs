using Fixawyy.Model_Views;
using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.FacilityAdmin}")]
public class LocationController : Controller
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        return View(await _locationService.GetAllAsync(cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var model = await _locationService.GetDetailsAsync(id, cancellationToken);
        return model == null ? NotFound() : View(model);
    }

    [HttpGet]
    public IActionResult Create() => View(new LocationFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LocationFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _locationService.CreateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Name), result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Location created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var location = await _locationService.GetDetailsAsync(id, cancellationToken);
        if (location == null)
        {
            return NotFound();
        }

        return View(new LocationFormViewModel
        {
            Id = location.Location.Id,
            Name = location.Location.Name,
            Description = location.Location.Description,
            Building = location.Location.Building,
            Floor = location.Location.Floor
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LocationFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _locationService.UpdateAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(nameof(model.Name), result.Error!);
            return View(model);
        }

        TempData["SuccessMessage"] = "Location updated successfully.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _locationService.DeleteAsync(id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Error;
        }
        else
        {
            TempData["SuccessMessage"] = "Location deleted successfully.";
        }

        return RedirectToAction(nameof(Index));
    }
}
