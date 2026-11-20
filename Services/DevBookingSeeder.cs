using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Fixawyy.Services;

/// <summary>
/// Development-only helper for the "mock IDs" testing mode (CustomerId "2", TechnicianId 1).
/// Never runs outside the Development environment and never when DevTesting:Enabled is false.
/// It does not touch DbInitializer: the existing seed data is left exactly as it is.
/// </summary>
public static class DevBookingSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<DevTestingOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DevBookingSeeder");

        if (!env.IsDevelopment() || !options.Enabled) return;

        var context = scope.ServiceProvider.GetRequiredService<Context.AppDbContext>();

        // Does the mock technician exist? (We only report it; we never create technicians.)
        if (!await context.TechnicianProfiles.AnyAsync(t => t.Id == options.TechnicianId))
        {
            logger.LogWarning(
                "DevTesting: TechnicianProfile with Id {TechnicianId} does not exist. Change DevTesting:TechnicianId to an existing profile id.",
                options.TechnicianId);
        }

        if (!options.SeedMockCustomer) return;

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (await userManager.FindByIdAsync(options.CustomerId) != null)
        {
            await SeedCompletedBookingAsync(context, options, logger);
            return;
        }

        const string email = "customer.test@fixawyy.com";
        if (await userManager.FindByEmailAsync(email) != null)
        {
            logger.LogWarning("DevTesting: {Email} already exists with a different id; mock customer '{Id}' was not created.", email, options.CustomerId);
            return;
        }

        if (!await roleManager.RoleExistsAsync(AppRoles.Customer))
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Customer));

        var user = new ApplicationUser
        {
            Id = options.CustomerId,
            UserName = email,
            Email = email,
            FullName = "Test Customer (dev mock)",
            PhoneNumber = "+201000000002",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, "Customer@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, AppRoles.Customer);
            logger.LogInformation("DevTesting: created mock customer with Id '{Id}' ({Email}).", options.CustomerId, email);
            await SeedCompletedBookingAsync(context, options, logger);
        }
        else
        {
            logger.LogWarning("DevTesting: could not create mock customer: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Gives the mock customer one COMPLETED, not-yet-reviewed booking in the past so the review flow can be tested
    /// (a booking cannot be completed through the UI before its start time).
    /// </summary>
    private static async Task SeedCompletedBookingAsync(Context.AppDbContext context, DevTestingOptions options, ILogger logger)
    {
        if (!await context.Users.AnyAsync(u => u.Id == options.CustomerId)) return;
        if (await context.Bookings.AnyAsync(b => b.CustomerId == options.CustomerId)) return;

        var tech = await context.TechnicianProfiles
            .Include(t => t.TechnicianSpecialties).ThenInclude(ts => ts.Specialty)
            .FirstOrDefaultAsync(t => t.Id == options.TechnicianId);
        var specialty = tech?.TechnicianSpecialties.Select(ts => ts.Specialty).FirstOrDefault();
        if (tech == null || specialty == null) return;

        var booking = new Booking
        {
            CustomerId = options.CustomerId,
            TechnicianId = tech.Id,
            BookingDate = DateTime.UtcNow.Date.AddDays(-7),
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            Status = BookingStatus.Completed,
            Notes = "Dev seed: completed booking for testing reviews.",
            TotalPrice = tech.HourlyRate,
            CreatedAt = DateTime.UtcNow.AddDays(-8)
        };
        booking.Items.Add(new BookingItem { SpecialtyId = specialty.Id, ServiceName = specialty.Name, Price = tech.HourlyRate, DurationMinutes = 60 });

        context.Bookings.Add(booking);
        await context.SaveChangesAsync();
        logger.LogInformation("DevTesting: seeded completed booking #{Id} for review testing.", booking.Id);
    }
}
