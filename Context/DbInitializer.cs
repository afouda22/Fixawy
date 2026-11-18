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

        // 2. Default System Admin
        var adminEmail = "admin@fixawyy.com";
        var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
        if (existingAdmin == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Super Administrator",
                EmailConfirmed = true,
                IsActive = true,
                Facility = null, // Super Admin sees all facilities
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123456");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
            }
        }

        // 2.1 Facility Admins (Admins of Employees per Facility)
        var facilityAdmins = new[]
        {
            new { Email = "engineering.admin@fixawyy.com", Name = "Eng. Tamer Refaat (Faculty Admin - Engineering)", Role = AppRoles.FacilityAdmin, Facility = "Faculty of Engineering" },
            new { Email = "mall.admin@fixawyy.com", Name = "Hassan Sherif (Facility Admin - Mall Food Court)", Role = AppRoles.FacilityAdmin, Facility = "Mall Food Court" }
        };

        foreach (var fa in facilityAdmins)
        {
            var existingFa = await userManager.FindByEmailAsync(fa.Email);
            if (existingFa == null)
            {
                var faUser = new ApplicationUser
                {
                    UserName = fa.Email,
                    Email = fa.Email,
                    FullName = fa.Name,
                    EmailConfirmed = true,
                    IsActive = true,
                    Facility = fa.Facility,
                    CreatedAt = DateTime.UtcNow
                };

                var res = await userManager.CreateAsync(faUser, "Admin@123456");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(faUser, fa.Role);
                }
            }
            else if (string.IsNullOrEmpty(existingFa.Facility))
            {
                existingFa.Facility = fa.Facility;
                await userManager.UpdateAsync(existingFa);
            }
        }

        // 2.2 Regular Facility Employee (Submits complaints only, NOT allowed to dispatch)
        var employeeEmail = "employee@fixawyy.com";
        var existingEmployee = await userManager.FindByEmailAsync(employeeEmail);
        if (existingEmployee == null)
        {
            var empUser = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                FullName = "Kareem Adel (Staff Employee - Engineering Dept)",
                EmailConfirmed = true,
                IsActive = true,
                Facility = "Faculty of Engineering",
                CreatedAt = DateTime.UtcNow
            };

            var empRes = await userManager.CreateAsync(empUser, "Employee@123456");
            if (empRes.Succeeded)
            {
                await userManager.AddToRoleAsync(empUser, AppRoles.Employee);
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

        var plumbingSpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Plumbing");
        var electricalSpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Electrical");
        var hvacSpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name.StartsWith("HVAC"));
        var carpentrySpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Carpentry");
        var paintingSpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Painting");
        var applianceSpec = await context.Specialties.FirstOrDefaultAsync(s => s.Name == "Appliance Repair");

        // 4. Demo Technicians Seed
        var techData = new[]
        {
            new { Email = "technician@fixawyy.com", Name = "Ahmed Mahmoud (Lead Electrician)", Phone = "+201001234567", Rate = 250m, Exp = 8, Rating = 4.9, Reviews = 38, Specs = new[] { electricalSpec, hvacSpec } },
            new { Email = "khaled.plumber@fixawyy.com", Name = "Khaled Ibrahim (Master Plumber)", Phone = "+201112223344", Rate = 200m, Exp = 6, Rating = 4.7, Reviews = 29, Specs = new[] { plumbingSpec, applianceSpec } },
            new { Email = "youssef.hvac@fixawyy.com", Name = "Mohamed Youssef (HVAC Specialist)", Phone = "+201223334455", Rate = 220m, Exp = 5, Rating = 4.8, Reviews = 31, Specs = new[] { hvacSpec } },
            new { Email = "tarek.carpentry@fixawyy.com", Name = "Omar Tarek (Wood & Locks Expert)", Phone = "+201099887766", Rate = 180m, Exp = 7, Rating = 4.6, Reviews = 22, Specs = new[] { carpentrySpec, paintingSpec } },
            new { Email = "sayed.power@fixawyy.com", Name = "Sayed Hassan (Power & Circuits)", Phone = "+201556677889", Rate = 210m, Exp = 4, Rating = 4.5, Reviews = 19, Specs = new[] { electricalSpec } }
        };

        foreach (var td in techData)
        {
            var user = await userManager.FindByEmailAsync(td.Email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = td.Email,
                    Email = td.Email,
                    FullName = td.Name,
                    PhoneNumber = td.Phone,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var createRes = await userManager.CreateAsync(user, "Tech@123456");
                if (createRes.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, AppRoles.Technician);

                    var profile = new TechnicianProfile
                    {
                        UserId = user.Id,
                        Bio = $"Professional technician specializing in maintenance and rapid incident resolution with {td.Exp} years experience.",
                        HourlyRate = td.Rate,
                        YearsOfExperience = td.Exp,
                        IsAvailable = true,
                        Rating = td.Rating,
                        ReviewCount = td.Reviews
                    };

                    foreach (var sp in td.Specs)
                    {
                        if (sp != null)
                        {
                            profile.TechnicianSpecialties.Add(new TechnicianSpecialty { SpecialtyId = sp.Id });
                        }
                    }

                    for (int day = 0; day <= 4; day++)
                    {
                        profile.Schedules.Add(new TechnicianSchedule
                        {
                            DayOfWeek = (DayOfWeek)day,
                            StartTime = new TimeSpan(9, 0, 0),
                            EndTime = new TimeSpan(17, 0, 0),
                            IsWorkingDay = true
                        });
                    }

                    await context.TechnicianProfiles.AddAsync(profile);
                    await context.SaveChangesAsync();
                }
            }
        }

        // 5. Seed Maintenance Tickets
        if (!await context.MaintenanceTickets.AnyAsync())
        {
            var now = DateTime.UtcNow;
            var tickets = new List<MaintenanceTicket>
            {
                // Pending Tickets
                new() { Title = "Main Server Room AC Compressor Failure", Description = "Primary cooling unit tripped. Server room temp reached 32°C.", Department = "IT Data Center", RequiredSkill = "HVAC / Air Conditioning", Priority = TicketPriority.Critical, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-45) },
                new() { Title = "High-Voltage Distribution Panel Sparking", Description = "Intermittent arching and buzzing in panel B-4 on Floor 2.", Department = "Faculty of Engineering", RequiredSkill = "Electrical", Priority = TicketPriority.Critical, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-1) },
                new() { Title = "Burst Water Supply Pipe in Restroom 3B", Description = "Continuous leak flooding hallway and threatening carpeted office space.", Department = "Building A - Floor 3", RequiredSkill = "Plumbing", Priority = TicketPriority.High, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-2) },
                new() { Title = "Emergency Fire Exit Door Jammed", Description = "Hydraulic hinge is stuck preventing door from fully latching closed.", Department = "North Stairwell", RequiredSkill = "Carpentry", Priority = TicketPriority.High, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-3) },
                new() { Title = "Cafeteria Commercial Dishwasher Leak", Description = "Water pooling under drainage hose during wash cycle.", Department = "Mall Food Court", RequiredSkill = "Appliance Repair", Priority = TicketPriority.Medium, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-4) },
                new() { Title = "Fluorescent Light Fixture Flickering", Description = "Flickering ballasts causing eye strain in customer service lobby.", Department = "Front Office", RequiredSkill = "Electrical", Priority = TicketPriority.Low, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-6) },
                new() { Title = "Executive Meeting Room Wall Scuffs Repair", Description = "Wall plaster chipped and paint faded before VIP board meeting.", Department = "Executive Suite", RequiredSkill = "Painting", Priority = TicketPriority.Medium, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-7) },
                new() { Title = "Breakroom Ice Machine Not Freezing", Description = "Compressor runs but water tray does not produce ice blocks.", Department = "Staff Lounge", RequiredSkill = "Appliance Repair", Priority = TicketPriority.Low, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-8) },

                // In-Progress / Assigned Tickets
                new() { Title = "Central Chiller Water Temperature High", Description = "Supply water temperature 4 degrees above operating threshold.", Department = "Central Utility Plant", RequiredSkill = "HVAC / Air Conditioning", Priority = TicketPriority.High, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-5) },
                new() { Title = "Underground Parking Sump Pump Failure", Description = "Water level sensor alarmed during overnight rainfall.", Department = "Basement Parking B2", RequiredSkill = "Plumbing", Priority = TicketPriority.Critical, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-6) },
                new() { Title = "Elevator Emergency Call Button Inactive", Description = "Intercom speaker is quiet and alarm buzzer not responding.", Department = "Tower Elevator #2", RequiredSkill = "Electrical", Priority = TicketPriority.High, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-9) },
                new() { Title = "Reception Sliding Glass Door Track Blocked", Description = "Rollers off alignment causing door to stick halfway.", Department = "Main Lobby", RequiredSkill = "Carpentry", Priority = TicketPriority.Medium, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-11) },
                new() { Title = "Dental Clinic Autoclave Power Cut", Description = "Sterilizer trips circuit breaker immediately upon power up.", Department = "Medical Center", RequiredSkill = "Electrical", Priority = TicketPriority.Critical, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-12) },

                // Resolved Tickets (for Analytics)
                new() { Title = "Water Pressure Drop in Labs 401-404", Description = "Booster pump diaphragm had stuck valve.", Department = "Faculty of Science", RequiredSkill = "Plumbing", Priority = TicketPriority.High, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-2), ResolvedAt = now.AddDays(-2).AddHours(2.5) },
                new() { Title = "Outdoor Security Floodlights Short Circuit", Description = "Water ingress in junction box resolved with silicone sealing.", Department = "Perimeter Fence", RequiredSkill = "Electrical", Priority = TicketPriority.Medium, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-3), ResolvedAt = now.AddDays(-3).AddHours(1.8) },
                new() { Title = "Library Reading Hall AC Blower Belt Snapped", Description = "Fan belt replaced and pulleys re-aligned.", Department = "Central Library", RequiredSkill = "HVAC / Air Conditioning", Priority = TicketPriority.Medium, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-4), ResolvedAt = now.AddDays(-4).AddHours(3.2) },
                new() { Title = "Main Conference Room Audio Rack Power Cut", Description = "UPS battery fuse blown and replaced with 16A spare.", Department = "Auditorium", RequiredSkill = "Electrical", Priority = TicketPriority.High, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-5), ResolvedAt = now.AddDays(-5).AddHours(1.2) },
                new() { Title = "Office Kitchen Sink Trap Clogged", Description = "Grease trap cleared and chemical flush applied.", Department = "Finance Dept", RequiredSkill = "Plumbing", Priority = TicketPriority.Low, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-6), ResolvedAt = now.AddDays(-6).AddHours(1.0) }
            };

            await context.MaintenanceTickets.AddRangeAsync(tickets);
            await context.SaveChangesAsync();

            // 6. Seed Initial Assignments
            var techProfiles = await context.TechnicianProfiles.ToListAsync();
            if (techProfiles.Count >= 3)
            {
                var assignedTickets = tickets.Where(t => t.Status == TicketStatus.Assigned).ToList();
                for (int i = 0; i < assignedTickets.Count; i++)
                {
                    var chosenTech = techProfiles[i % techProfiles.Count];
                    context.Assignments.Add(new Assignment
                    {
                        TicketId = assignedTickets[i].Id,
                        TechnicianProfileId = chosenTech.Id,
                        AssignedAt = now.AddHours(-i - 1),
                        Status = "Active",
                        MatchScore = 92.5 - (i * 3),
                        Notes = "Assigned via Facility Dispatcher Engine"
                    });
                }

                var resolvedTickets = tickets.Where(t => t.Status == TicketStatus.Resolved).ToList();
                for (int i = 0; i < resolvedTickets.Count; i++)
                {
                    var chosenTech = techProfiles[i % techProfiles.Count];
                    context.Assignments.Add(new Assignment
                    {
                        TicketId = resolvedTickets[i].Id,
                        TechnicianProfileId = chosenTech.Id,
                        AssignedAt = resolvedTickets[i].CreatedAt,
                        CompletedAt = resolvedTickets[i].ResolvedAt,
                        Status = "Completed",
                        MatchScore = 95.0,
                        Notes = "Resolved successfully on schedule"
                    });
                }

                // 7. Seed Initial Notifications
                context.NotificationHistories.AddRange(
                    new NotificationHistory
                    {
                        TargetRole = "FacilityAdmin",
                        Title = "System Initialized",
                        Message = "Facility Operations Dispatcher Engine loaded successfully with 18 seed tickets.",
                        EventType = "NewNotification",
                        IsRead = true,
                        CreatedAt = now.AddHours(-24)
                    },
                    new NotificationHistory
                    {
                        TargetRole = "Technician",
                        Title = "Critical Ticket Inbound",
                        Message = "Server Room AC Compressor Failure flagged as Critical priority in IT Data Center.",
                        EventType = "NewAssignment",
                        RelatedTicketId = tickets[0].Id,
                        IsRead = false,
                        CreatedAt = now.AddMinutes(-40)
                    },
                    new NotificationHistory
                    {
                        TargetRole = "All",
                        Title = "Performance Milestone",
                        Message = "Average incident resolution time improved to under 2 hours this week.",
                        EventType = "StatusChanged",
                        IsRead = false,
                        CreatedAt = now.AddHours(-2)
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
