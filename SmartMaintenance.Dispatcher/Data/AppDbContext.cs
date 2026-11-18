using Microsoft.EntityFrameworkCore;
using SmartMaintenance.Dispatcher.Models;
using System.Reflection.Emit;

namespace SmartMaintenance.Dispatcher.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Technician> Technicians => Set<Technician>();
    public DbSet<MaintenanceTicket> Tickets => Set<MaintenanceTicket>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // إعداد العلاقات
        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Ticket)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Assignment>()
            .HasOne(a => a.Technician)
            .WithMany(t => t.Assignments)
            .HasForeignKey(a => a.TechnicianId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}