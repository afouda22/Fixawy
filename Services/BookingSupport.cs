using System.Data;
using System.Security.Claims;
using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fixawyy.Services;

// ---------------------------------------------------------------------------------------------
// Teammate 2 support classes: development testing configuration, current-user resolution,
// time helpers and the per-resource database lock used to prevent double booking.
// ---------------------------------------------------------------------------------------------

/// <summary>
/// Central place for the development "mock identity" settings (section "DevTesting" in appsettings).
/// Only honoured when the app runs in the Development environment AND nobody is signed in.
/// </summary>
public class DevTestingOptions
{
    /// <summary>Master switch. Default false so production is never affected.</summary>
    public bool Enabled { get; set; }

    /// <summary>Identity user id used as the mock customer.</summary>
    public string CustomerId { get; set; } = "2";

    /// <summary>TechnicianProfile.Id used as the mock technician.</summary>
    public int TechnicianId { get; set; } = 1;

    /// <summary>"Customer" or "Technician": which role the anonymous dev visitor acts as.</summary>
    public string ActAs { get; set; } = "Customer";

    /// <summary>Create the mock customer user (Id = CustomerId) at startup if it does not exist.</summary>
    public bool SeedMockCustomer { get; set; }
}

/// <summary>Who is making the current request, as far as the booking engine is concerned.</summary>
public class BookingActor
{
    public string UserId { get; init; } = string.Empty;
    public bool IsCustomer { get; init; }
    public bool IsTechnician { get; init; }
    public int? TechnicianProfileId { get; init; }

    /// <summary>True when the identity comes from the DevTesting configuration instead of a login.</summary>
    public bool IsMock { get; init; }
    public string DisplayName { get; init; } = string.Empty;
}

public interface ICurrentActorProvider
{
    /// <summary>Returns the acting user, or null when nobody is signed in (and no dev mock applies).</summary>
    Task<BookingActor?> GetCurrentAsync(ClaimsPrincipal principal);
}

public class CurrentActorProvider : ICurrentActorProvider
{
    private readonly Context.AppDbContext _context;
    private readonly IHostEnvironment _environment;
    private readonly DevTestingOptions _dev;

    public CurrentActorProvider(Context.AppDbContext context, IHostEnvironment environment, IOptions<DevTestingOptions> dev)
    {
        _context = context;
        _environment = environment;
        _dev = dev.Value;
    }

    public async Task<BookingActor?> GetCurrentAsync(ClaimsPrincipal principal)
    {
        // 1. A real signed-in user always wins.
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return null;

            var isTechnician = principal.IsInRole(AppRoles.Technician);
            int? techId = null;
            if (isTechnician)
            {
                techId = await _context.TechnicianProfiles
                    .Where(t => t.UserId == userId)
                    .Select(t => (int?)t.Id)
                    .FirstOrDefaultAsync();
            }

            return new BookingActor
            {
                UserId = userId,
                IsCustomer = principal.IsInRole(AppRoles.Customer),
                IsTechnician = isTechnician && techId.HasValue,
                TechnicianProfileId = techId,
                DisplayName = principal.Identity?.Name ?? userId
            };
        }

        // 2. Development-only mock identity (never in Production).
        if (_environment.IsDevelopment() && _dev.Enabled)
        {
            if (string.Equals(_dev.ActAs, AppRoles.Technician, StringComparison.OrdinalIgnoreCase))
            {
                var tech = await _context.TechnicianProfiles
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == _dev.TechnicianId);
                if (tech == null) return null;

                return new BookingActor
                {
                    UserId = tech.UserId,
                    IsTechnician = true,
                    TechnicianProfileId = tech.Id,
                    IsMock = true,
                    DisplayName = $"{tech.User.FullName} (technician #{tech.Id})"
                };
            }

            return new BookingActor
            {
                UserId = _dev.CustomerId,
                IsCustomer = true,
                IsMock = true,
                DisplayName = $"mock customer '{_dev.CustomerId}'"
            };
        }

        return null;
    }
}

/// <summary>Clock abstraction so "now" is the business time zone (Egypt) and can be replaced in tests.</summary>
public interface IBookingClock
{
    /// <summary>Current local business time (Africa/Cairo).</summary>
    DateTime Now { get; }
}

public class BookingClock : IBookingClock
{
    private static readonly TimeZoneInfo Zone = ResolveZone();

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); } catch { /* try next id */ }
        }
        return TimeZoneInfo.Local;
    }

    public DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, Zone);
}

/// <summary>SQL Server application lock: serialises work on one logical resource (e.g. one technician).</summary>
internal static class DbLock
{
    /// <summary>
    /// Must be called inside an open transaction. The lock is released automatically on commit/rollback.
    /// Does nothing on providers other than SQL Server.
    /// </summary>
    public static async Task AcquireAsync(Context.AppDbContext context, string resource, CancellationToken ct = default)
    {
        if (!context.Database.IsSqlServer()) return;

        var result = new Microsoft.Data.SqlClient.SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var res = new Microsoft.Data.SqlClient.SqlParameter("@res", resource);

        await context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @res, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;",
            new object[] { result, res }, ct);

        if (result.Value is int code && code < 0)
        {
            throw new TimeoutException("Could not obtain the booking lock. Please try again.");
        }
    }
}
