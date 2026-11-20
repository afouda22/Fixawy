using System.ComponentModel.DataAnnotations;
using Fixawyy.Models;

namespace Fixawyy.Model_Views;

// ---------------------------------------------------------------------------------------------
// Teammate 2: Booking & Review Engine view models
// ---------------------------------------------------------------------------------------------

/// <summary>Filters for the public technician catalog (reuses TechnicianCardViewModel for results).</summary>
public class TechnicianCatalogFilter
{
    public int? SpecialtyId { get; set; }
    public string? Search { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public bool OnlyAvailable { get; set; } = true;
}

/// <summary>Existing card plus the working-hours information needed for the catalog.</summary>
public class CatalogTechnicianCardViewModel : TechnicianCardViewModel
{
    public List<string> WorkingDays { get; set; } = new();
    public string WorkingHours { get; set; } = string.Empty;
}

public class TechnicianCatalogViewModel
{
    public List<CatalogTechnicianCardViewModel> Technicians { get; set; } = new();
    public List<Specialty> Specialties { get; set; } = new();
    public TechnicianCatalogFilter Filter { get; set; } = new();
}

public class ServiceOptionViewModel
{
    public int SpecialtyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>Booking form: technician summary (display only) + what the customer submits.</summary>
public class CreateBookingViewModel
{
    // --- submitted by the customer ---
    [Required]
    public int TechnicianId { get; set; }

    [Required(ErrorMessage = "Please select at least one service.")]
    [MinLength(1, ErrorMessage = "Please select at least one service.")]
    [Display(Name = "Services")]
    public List<int> SelectedSpecialtyIds { get; set; } = new();

    [Required(ErrorMessage = "Please choose a date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateTime? BookingDate { get; set; }

    [Required(ErrorMessage = "Please choose a time slot.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Invalid time format.")]
    [Display(Name = "Time slot")]
    public string? StartTime { get; set; }

    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters.")]
    [Display(Name = "Notes for the technician")]
    public string? Notes { get; set; }

    // --- display only (re-filled by the server) ---
    public string TechnicianName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public decimal HourlyRate { get; set; }
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public int MaxAdvanceDays { get; set; }
    public List<ServiceOptionViewModel> Services { get; set; } = new();
}

public class TimeSlotViewModel
{
    public string Value { get; set; } = string.Empty;   // "HH:mm"
    public string Label { get; set; } = string.Empty;   // "09:00 AM - 10:00 AM"
}

public class SlotsResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<TimeSlotViewModel> Slots { get; set; } = new();
}

public class BookingItemViewModel
{
    public string ServiceName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

public class BookingListItemViewModel
{
    public int Id { get; set; }
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string? TechnicianPictureUrl { get; set; }
    public string? TechnicianPhone { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public BookingStatus Status { get; set; }
    public string? Notes { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<BookingItemViewModel> Items { get; set; } = new();

    public bool HasReview { get; set; }
    public bool CanCancel { get; set; }
    public bool CanReview { get; set; }

    /// <summary>Statuses the signed-in technician may move this booking to (empty for customers).</summary>
    public List<BookingStatus> AllowedNextStatuses { get; set; } = new();

    public string TimeRangeText => $"{DateTime.Today.Add(StartTime):hh:mm tt} - {DateTime.Today.Add(EndTime):hh:mm tt}";
}

public class UpdateBookingStatusViewModel
{
    [Required]
    public int BookingId { get; set; }

    [Required]
    public BookingStatus NewStatus { get; set; }
}

public class ReviewFormViewModel
{
    [Required]
    public int BookingId { get; set; }

    [Required(ErrorMessage = "Please choose a star rating.")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int? Rating { get; set; }

    [MaxLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters.")]
    public string? Comment { get; set; }

    // display only
    public string TechnicianName { get; set; } = string.Empty;
    public string? TechnicianPictureUrl { get; set; }
    public DateTime BookingDate { get; set; }
    public string ServicesText { get; set; } = string.Empty;
}

public class ReviewItemViewModel
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TechnicianReviewsViewModel
{
    public int TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string? ProfilePictureUrl { get; set; }
    public decimal HourlyRate { get; set; }
    public bool IsAvailable { get; set; }
    public List<string> Specialties { get; set; } = new();

    /// <summary>Value stored on TechnicianProfile (what the directory shows).</summary>
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    /// <summary>Number of written reviews per star (index 1..5) from the Reviews table.</summary>
    public int[] StarCounts { get; set; } = new int[6];
    public List<ReviewItemViewModel> Reviews { get; set; } = new();
}
