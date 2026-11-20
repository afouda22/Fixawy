using System.Globalization;
using Fixawyy.Model_Views;
using Fixawyy.Models;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace Fixawyy.Controllers;

/// <summary>
/// Teammate 2: customer booking flow, customer "My Bookings", technician booking management and the public catalog.
/// The signed-in user comes from ICurrentActorProvider (real login, or the DevTesting mock in Development only).
/// </summary>
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly ITechnicianCatalogService _catalogService;
    private readonly ICurrentActorProvider _actors;

    public BookingController(IBookingService bookingService, ITechnicianCatalogService catalogService, ICurrentActorProvider actors)
    {
        _bookingService = bookingService;
        _catalogService = catalogService;
        _actors = actors;
    }

    // ------------------------------------------------------------ public catalog

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Technicians()
    {
        // Find a Technician (Technician/Directory) is the only discovery page; keep old links/bookmarks working.
        var routeValues = new RouteValueDictionary();
        foreach (var kv in Request.Query) routeValues[kv.Key] = kv.Value.ToString();
        return RedirectToAction("Directory", "Technician", routeValues);
    }

    // ------------------------------------------------------------ create booking

    [HttpGet]
    public async Task<IActionResult> Create(int technicianId)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        SetDevBanner(actor);

        var model = await _bookingService.GetBookingFormAsync(technicianId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        SetDevBanner(actor);

        if (ModelState.IsValid)
        {
            // The customer id always comes from the server-side identity, never from the form.
            var result = await _bookingService.CreateBookingAsync(actor.UserId, model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Your booking request was sent to the technician.";
                return RedirectToAction(nameof(Confirmation), new { id = result.BookingId });
            }
            ModelState.AddModelError(string.Empty, result.Error ?? "The booking could not be created.");
        }

        var display = await _bookingService.GetBookingFormAsync(model.TechnicianId);
        if (display == null) return NotFound();

        // Keep what the customer typed, refresh the display-only fields.
        display.SelectedSpecialtyIds = model.SelectedSpecialtyIds;
        display.BookingDate = model.BookingDate;
        display.StartTime = model.StartTime;
        display.Notes = model.Notes;
        return View(display);
    }

    /// <summary>JSON used by the booking page to refresh time slots when a date is picked.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Slots(int technicianId, string? date, int services = 1)
    {
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return Json(new SlotsResponse { Success = false, Message = "Please choose a valid date." });

        var response = await _bookingService.GetAvailableSlotsAsync(technicianId, parsed, services);
        return Json(response);
    }

    [HttpGet]
    public async Task<IActionResult> Confirmation(int id)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        SetDevBanner(actor);

        var booking = await _bookingService.GetCustomerBookingAsync(actor.UserId, id);
        if (booking == null) return NotFound();
        return View(booking);
    }

    // ------------------------------------------------------------ customer: my bookings

    [HttpGet]
    public async Task<IActionResult> MyBookings()
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        SetDevBanner(actor);

        var bookings = await _bookingService.GetCustomerBookingsAsync(actor.UserId);
        return View(bookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();

        var result = await _bookingService.UpdateBookingStatusAsync(id, BookingStatus.Cancelled, actor);
        if (result.Outcome == BookingOutcome.NotFound) return NotFound();

        if (result.Success) TempData["SuccessMessage"] = "Your booking was cancelled.";
        else TempData["ErrorMessage"] = result.Error ?? "The booking could not be cancelled.";

        return RedirectToAction(nameof(MyBookings));
    }

    // ------------------------------------------------------------ technician: assigned bookings

    [HttpGet]
    public async Task<IActionResult> TechnicianBookings()
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsTechnician || actor.TechnicianProfileId == null) return Forbid();
        SetDevBanner(actor);

        var bookings = await _bookingService.GetTechnicianBookingsAsync(actor.TechnicianProfileId.Value);
        return View(bookings);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateBookingStatusViewModel model)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsTechnician) return Forbid();

        if (!ModelState.IsValid || !Enum.IsDefined(model.NewStatus))
        {
            TempData["ErrorMessage"] = "Invalid status update request.";
            return RedirectToAction(nameof(TechnicianBookings));
        }

        var result = await _bookingService.UpdateBookingStatusAsync(model.BookingId, model.NewStatus, actor);
        if (result.Outcome == BookingOutcome.NotFound) return NotFound();

        if (result.Success) TempData["SuccessMessage"] = $"Booking #{model.BookingId} is now {model.NewStatus}.";
        else TempData["ErrorMessage"] = result.Error ?? "The status could not be updated.";

        return RedirectToAction(nameof(TechnicianBookings));
    }

    private void SetDevBanner(BookingActor actor)
    {
        ViewData["DevActor"] = actor.IsMock ? actor.DisplayName : null;
    }
}
