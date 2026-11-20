using System.ComponentModel.DataAnnotations;

namespace Fixawyy.Models;

/// <summary>
/// A B2C appointment between a customer (ApplicationUser) and a technician (TechnicianProfile).
/// Separate from MaintenanceTicket / Assignment (the facility dispatcher workflow).
/// </summary>
public class Booking
{
    public int Id { get; set; }

    public string CustomerId { get; set; } = string.Empty;
    public virtual ApplicationUser Customer { get; set; } = null!;

    public int TechnicianId { get; set; }
    public virtual TechnicianProfile Technician { get; set; } = null!;

    /// <summary>Calendar date of the appointment (time part is always 00:00).</summary>
    public DateTime BookingDate { get; set; }

    /// <summary>Appointment start, as time of day on <see cref="BookingDate"/>.</summary>
    public TimeSpan StartTime { get; set; }

    /// <summary>Appointment end, as time of day on <see cref="BookingDate"/>.</summary>
    public TimeSpan EndTime { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual ICollection<BookingItem> Items { get; set; } = new List<BookingItem>();
    public virtual Review? Review { get; set; }
}

/// <summary>A service line inside a booking. Services are the technician's Specialties.</summary>
public class BookingItem
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public virtual Booking Booking { get; set; } = null!;

    /// <summary>Reference to the existing Specialty (nullable so a deleted specialty does not erase history).</summary>
    public int? SpecialtyId { get; set; }
    public virtual Specialty? Specialty { get; set; }

    /// <summary>Snapshot of the service name at booking time.</summary>
    [MaxLength(100)]
    public string ServiceName { get; set; } = string.Empty;

    /// <summary>Snapshot of the price at booking time (hourly rate x hours).</summary>
    public decimal Price { get; set; }

    public int DurationMinutes { get; set; } = 60;
}

/// <summary>One review per completed booking.</summary>
public class Review
{
    public int Id { get; set; }

    public int BookingId { get; set; }
    public virtual Booking Booking { get; set; } = null!;

    public string CustomerId { get; set; } = string.Empty;
    public virtual ApplicationUser Customer { get; set; } = null!;

    public int TechnicianId { get; set; }
    public virtual TechnicianProfile Technician { get; set; } = null!;

    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
