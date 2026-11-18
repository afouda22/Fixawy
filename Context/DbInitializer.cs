using Fixawyy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Context;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // 1. Roles
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // 2. Default Admin
        var adminEmail = "admin@fixawyy.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        // 3. Specialties
        if (!await context.Specialties.AnyAsync())
        {
            var specialties = new List<Specialty>
            {
                new() { Name = "Plumbing", Description = "Pipe repairs, drain cleaning, leaks, and sanitary installations", IconUrl = "bi-tools" },
                new() { Name = "Electrical", Description = "Wiring, lighting, circuit breakers, and power outlets", IconUrl = "bi-lightning-charge" },
                new() { Name = "HVAC / Air Conditioning", Description = "AC maintenance, coolant recharge, heating, and ventilation", IconUrl = "bi-snow" },
                new() { Name = "Carpentry", Description = "Furniture assembly, door lock repairs, cabinets, and woodwork", IconUrl = "bi-hammer" },
                new() { Name = "Painting", Description = "Interior and exterior wall painting and surface finishing", IconUrl = "bi-brush" },
                new() { Name = "Appliance Repair", Description = "Washing machines, refrigerators, and kitchen appliances", IconUrl = "bi-cpu" }
            };

            await context.Specialties.AddRangeAsync(specialties);
            await context.SaveChangesAsync();
        }

        // 4. Demo Technician
        var techEmail = "technician@fixawyy.com";
        var existingTech = await userManager.FindByEmailAsync(techEmail);
        if (existingTech == null)
        {
            var techUser = new ApplicationUser
            {
                UserName = techEmail,
                Email = techEmail,
                FullName = "Ahmed Mahmoud (Lead Electrician)",
                PhoneNumber = "+201001234567",
                EmailConfirmed = true,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(techUser, "Tech@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(techUser, AppRoles.Technician);

                var electricalSpecialty = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Electrical");
                var acSpecialty = await context.Specialties.FirstOrDefaultAsync(s => s.Name.StartsWith("HVAC"));

                var techProfile = new TechnicianProfile
                {
                    UserId = techUser.Id,
                    Bio = "Certified electrical and HVAC technician with 8+ years of hands-on experience in residential and corporate facility maintenance.",
                    HourlyRate = 250m,
                    YearsOfExperience = 8,
                    IsAvailable = true,
                    Rating = 4.9,
                    ReviewCount = 38
                };

                if (electricalSpecialty != null)
                {
                    techProfile.TechnicianSpecialties.Add(new TechnicianSpecialty { SpecialtyId = electricalSpecialty.Id });
                }
                if (acSpecialty != null)
                {
                    techProfile.TechnicianSpecialties.Add(new TechnicianSpecialty { SpecialtyId = acSpecialty.Id });
                }

                for (int day = 0; day <= 4; day++)
                {
                    techProfile.Schedules.Add(new TechnicianSchedule
                    {
                        DayOfWeek = (DayOfWeek)day,
                        StartTime = new TimeSpan(9, 0, 0),
                        EndTime = new TimeSpan(17, 0, 0),
                        IsWorkingDay = true
                    });
                }

                await context.TechnicianProfiles.AddAsync(techProfile);
                await context.SaveChangesAsync();
            }
        }
    }
}
