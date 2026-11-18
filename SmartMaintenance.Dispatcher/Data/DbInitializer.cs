using SmartMaintenance.Dispatcher.Models;

namespace SmartMaintenance.Dispatcher.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
       
        await context.Database.EnsureCreatedAsync();

      
        if (context.Technicians.Any())
        {
            return;
        }

        // 1. إضافة 5 فنيين بتخصصات وتقييمات وضغط عمل متنوع
        var technicians = new List<Technician>
        {
            new Technician { FullName = "Ahmed Mansour", Specialization = "Electrical", Rating = 4.9, IsAvailable = true, ActiveTicketsCount = 1, Email = "ahmed.m@company.com", Phone = "01011112222" },
            new Technician { FullName = "Mahmoud Hassan", Specialization = "Plumbing", Rating = 4.7, IsAvailable = true, ActiveTicketsCount = 0, Email = "mahmoud.h@company.com", Phone = "01033334444" },
            new Technician { FullName = "Tarek El-Sayed", Specialization = "HVAC", Rating = 4.8, IsAvailable = true, ActiveTicketsCount = 2, Email = "tarek.s@company.com", Phone = "01055556666" },
            new Technician { FullName = "Khaled Ibrahim", Specialization = "Carpentry", Rating = 4.6, IsAvailable = true, ActiveTicketsCount = 0, Email = "khaled.i@company.com", Phone = "01077778888" },
            new Technician { FullName = "Youssef Nabil", Specialization = "Electrical", Rating = 4.5, IsAvailable = false, ActiveTicketsCount = 3, Email = "youssef.n@company.com", Phone = "01099990000" } // غير متاح
        };

        await context.Technicians.AddRangeAsync(technicians);
        await context.SaveChangesAsync();

        // 2. إضافة 18 تذكرة صيانة متنوعة لحساب مؤشرات الأداء واختبار الخوارزمية
        var now = DateTime.UtcNow;
        var tickets = new List<MaintenanceTicket>
        {
            // تذاكر منتهية ومحلولة (Resolved) -> لحساب متوسط وقت الحل (Resolution Time)
            new MaintenanceTicket { Title = "Server Room AC Failure", Description = "AC leaking water and stopped cooling", Department = "IT Department", RequiredSkill = "HVAC", Priority = TicketPriority.Critical, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-6), ResolvedAt = now.AddDays(-6).AddHours(3) },
            new MaintenanceTicket { Title = "Main Panel Short Circuit", Description = "Breaker tripped in Building B", Department = "Building B", RequiredSkill = "Electrical", Priority = TicketPriority.Critical, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-5), ResolvedAt = now.AddDays(-5).AddHours(2) },
            new MaintenanceTicket { Title = "Restroom Pipe Burst", Description = "Water flooding 2nd floor restroom", Department = "Administration", RequiredSkill = "Plumbing", Priority = TicketPriority.High, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-4), ResolvedAt = now.AddDays(-4).AddHours(4) },
            new MaintenanceTicket { Title = "Office Door Hinge Broken", Description = "Door cannot close securely", Department = "HR Department", RequiredSkill = "Carpentry", Priority = TicketPriority.Low, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-3), ResolvedAt = now.AddDays(-3).AddHours(6) },
            new MaintenanceTicket { Title = "Lighting Malfunction in Hall", Description = "Flickering LED lights", Department = "Conference Hall", RequiredSkill = "Electrical", Priority = TicketPriority.Medium, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-2), ResolvedAt = now.AddDays(-2).AddHours(5) },
            new MaintenanceTicket { Title = "Chiller Filter Replacement", Description = "Routine filter maintenance", Department = "Mall - Level 1", RequiredSkill = "HVAC", Priority = TicketPriority.Medium, Status = TicketStatus.Resolved, CreatedAt = now.AddDays(-1), ResolvedAt = now.AddDays(-1).AddHours(4) },

            // تذاكر جارية قيد التنفيذ (InProgress)
            new MaintenanceTicket { Title = "Elevator Power Fluctuation", Description = "Voltage drop detected", Department = "Building A", RequiredSkill = "Electrical", Priority = TicketPriority.Critical, Status = TicketStatus.InProgress, CreatedAt = now.AddHours(-10) },
            new MaintenanceTicket { Title = "Cafeteria Sink Drainage Blocked", Description = "Water backs up in sink", Department = "Cafeteria", RequiredSkill = "Plumbing", Priority = TicketPriority.High, Status = TicketStatus.InProgress, CreatedAt = now.AddHours(-8) },
            new MaintenanceTicket { Title = "HVAC Strange Noise", Description = "Loud buzzing sound from central unit", Department = "Library", RequiredSkill = "HVAC", Priority = TicketPriority.Medium, Status = TicketStatus.InProgress, CreatedAt = now.AddHours(-6) },

            // تذاكر تم تعيينها ولكن لم تبدأ بعد (Assigned)
            new MaintenanceTicket { Title = "Window Frame Replacement", Description = "Wooden frame damaged by rain", Department = "Building C", RequiredSkill = "Carpentry", Priority = TicketPriority.Medium, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-4) },
            new MaintenanceTicket { Title = "Emergency Exit Light Replacement", Description = "Battery backup dead", Department = "Mall - Level 2", RequiredSkill = "Electrical", Priority = TicketPriority.High, Status = TicketStatus.Assigned, CreatedAt = now.AddHours(-3) },

            // تذاكر معلقة بانتظار التوزيع (Pending) -> هذه التي ستعمل عليها خوارزميتنا!
            new MaintenanceTicket { Title = "Main Water Supply Leak", Description = "Underground pipe dripping near garden", Department = "Facilities", RequiredSkill = "Plumbing", Priority = TicketPriority.Critical, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-2) },
            new MaintenanceTicket { Title = "Conference Room Projector Socket", Description = "No power reaching outlet", Department = "Conference Hall", RequiredSkill = "Electrical", Priority = TicketPriority.High, Status = TicketStatus.Pending, CreatedAt = now.AddHours(-1) },
            new MaintenanceTicket { Title = "Server Room Temperature High", Description = "Secondary AC unit not starting", Department = "IT Department", RequiredSkill = "HVAC", Priority = TicketPriority.Critical, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-45) },
            new MaintenanceTicket { Title = "Cabinet Lock Installation", Description = "Install security lock on storage", Department = "Finance", RequiredSkill = "Carpentry", Priority = TicketPriority.Low, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-30) },
            new MaintenanceTicket { Title = "Corridor Lights Off", Description = "4 lamps not turning on", Department = "Building B", RequiredSkill = "Electrical", Priority = TicketPriority.Medium, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-20) },
            new MaintenanceTicket { Title = "Executive Suite Toilet Issue", Description = "Flush mechanism broken", Department = "Administration", RequiredSkill = "Plumbing", Priority = TicketPriority.High, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-15) },
            new MaintenanceTicket { Title = "Auditorium Wooden Podium Repair", Description = "Broken leg on lecture podium", Department = "Auditorium", RequiredSkill = "Carpentry", Priority = TicketPriority.Low, Status = TicketStatus.Pending, CreatedAt = now.AddMinutes(-5) }
        };

        await context.Tickets.AddRangeAsync(tickets);
        await context.SaveChangesAsync();

        // 3. إضافة سجل التعيينات السابقة (Assignments)
        var assignments = new List<Assignment>
        {
            new Assignment { TicketId = tickets[0].Id, TechnicianId = technicians[2].Id, AssignedAt = now.AddDays(-6), CompletedAt = now.AddDays(-6).AddHours(3), Status = "Completed", MatchScore = 95 },
            new Assignment { TicketId = tickets[1].Id, TechnicianId = technicians[0].Id, AssignedAt = now.AddDays(-5), CompletedAt = now.AddDays(-5).AddHours(2), Status = "Completed", MatchScore = 98 },
            new Assignment { TicketId = tickets[6].Id, TechnicianId = technicians[0].Id, AssignedAt = now.AddHours(-9), Status = "Active", MatchScore = 94 },
            new Assignment { TicketId = tickets[7].Id, TechnicianId = technicians[1].Id, AssignedAt = now.AddHours(-7), Status = "Active", MatchScore = 92 },
            new Assignment { TicketId = tickets[8].Id, TechnicianId = technicians[2].Id, AssignedAt = now.AddHours(-5), Status = "Active", MatchScore = 89 }
        };

        await context.Assignments.AddRangeAsync(assignments);

        // 4. إضافة سجل إشعارات تجريبية (NotificationHistory)
        var notifications = new List<NotificationHistory>
        {
            new NotificationHistory { TargetRole = "Manager", Title = "Critical Ticket Created", Message = "Main Water Supply Leak requires urgent dispatch", EventType = "NewNotification", IsRead = false, CreatedAt = now.AddHours(-2) },
            new NotificationHistory { TargetRole = "Technician", RecipientUserId = technicians[0].Id, Title = "New Assignment", Message = "You have been assigned to: Elevator Power Fluctuation", EventType = "NewAssignment", IsRead = true, CreatedAt = now.AddHours(-9) },
            new NotificationHistory { TargetRole = "All", Title = "Ticket Resolved", Message = "Server Room AC Failure has been resolved by Tarek El-Sayed", EventType = "StatusChanged", IsRead = true, CreatedAt = now.AddDays(-6).AddHours(3) }
        };

        await context.NotificationHistories.AddRangeAsync(notifications);
        await context.SaveChangesAsync();
    }
}