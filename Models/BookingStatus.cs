namespace Fixawyy.Models;

/// <summary>
/// Lifecycle of a customer booking (Teammate 2 - Booking &amp; Review Engine).
/// The numeric values are stored in the database (the unique "active slot" index depends on Cancelled = 3).
/// </summary>
public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    Completed = 2,
    Cancelled = 3
}
