using Fixawyy.Model_Views;
using Fixawyy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fixawyy.Controllers;

/// <summary>Teammate 2: review submission (customers) and the public technician reviews page.</summary>
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly ICurrentActorProvider _actors;

    public ReviewController(IReviewService reviewService, ICurrentActorProvider actors)
    {
        _reviewService = reviewService;
        _actors = actors;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int bookingId)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        ViewData["DevActor"] = actor.IsMock ? actor.DisplayName : null;

        var model = await _reviewService.GetReviewFormAsync(bookingId, actor.UserId);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormViewModel model)
    {
        var actor = await _actors.GetCurrentAsync(User);
        if (actor == null) return Challenge();
        if (!actor.IsCustomer) return Forbid();
        ViewData["DevActor"] = actor.IsMock ? actor.DisplayName : null;

        if (ModelState.IsValid)
        {
            var result = await _reviewService.SubmitReviewAsync(actor.UserId, model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Thank you! Your review has been submitted.";
                return RedirectToAction("MyBookings", "Booking");
            }
            if (result.Outcome == BookingOutcome.NotFound) return NotFound();
            ModelState.AddModelError(string.Empty, result.Error ?? "The review could not be saved.");
        }

        // Re-fill the display-only fields (and keep the customer's input).
        var display = await _reviewService.GetReviewFormAsync(model.BookingId, actor.UserId);
        if (display == null) return NotFound();
        display.Rating = model.Rating;
        display.Comment = model.Comment;
        return View(display);
    }

    /// <summary>Public page: all reviews and the rating summary of one technician.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Technician(int id)
    {
        var model = await _reviewService.GetTechnicianReviewsAsync(id);
        if (model == null) return NotFound();
        return View(model);
    }
}
