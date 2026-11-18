using Fixawyy.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Context;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TechnicianProfile> TechnicianProfiles => Set<TechnicianProfile>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<TechnicianSpecialty> TechnicianSpecialties => Set<TechnicianSpecialty>();
    public DbSet<TechnicianSchedule> TechnicianSchedules => Set<TechnicianSchedule>();
    public DbSet<MaintenanceTicket> MaintenanceTickets => Set<MaintenanceTicket>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<NotificationHistory> NotificationHistories => Set<NotificationHistory>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FullName).HasMaxLength(150).IsRequired();
            entity.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(u => u.NationalId).HasMaxLength(14);
            entity.Property(u => u.Country).HasMaxLength(100);
            entity.Property(u => u.SecondaryPhoneNumber).HasMaxLength(30);
            entity.Property(u => u.VerificationCode).HasMaxLength(10);
            entity.Property(u => u.Facility).HasMaxLength(150);
        });

        builder.Entity<TechnicianProfile>(entity =>
        {
            entity.HasKey(tp => tp.Id);
            entity.Property(tp => tp.HourlyRate).HasColumnType("decimal(18,2)");
            entity.Property(tp => tp.Bio).HasMaxLength(1000);

            entity.HasOne(tp => tp.User)
                  .WithOne(u => u.TechnicianProfile)
                  .HasForeignKey<TechnicianProfile>(tp => tp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Specialty>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Description).HasMaxLength(500);
            entity.Property(s => s.IconUrl).HasMaxLength(255);
        });

        builder.Entity<TechnicianSpecialty>(entity =>
        {
            entity.HasKey(ts => new { ts.TechnicianProfileId, ts.SpecialtyId });

            entity.HasOne(ts => ts.TechnicianProfile)
                  .WithMany(tp => tp.TechnicianSpecialties)
                  .HasForeignKey(ts => ts.TechnicianProfileId);

            entity.HasOne(ts => ts.Specialty)
                  .WithMany(s => s.TechnicianSpecialties)
                  .HasForeignKey(ts => ts.SpecialtyId);
        });

        builder.Entity<TechnicianSchedule>(entity =>
        {
            entity.HasKey(ts => ts.Id);

            entity.HasOne(ts => ts.TechnicianProfile)
                  .WithMany(tp => tp.Schedules)
                  .HasForeignKey(ts => ts.TechnicianProfileId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MaintenanceTicket>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Title).HasMaxLength(200).IsRequired();
            entity.Property(t => t.Department).HasMaxLength(100);
            entity.Property(t => t.RequiredSkill).HasMaxLength(100);
        });

        builder.Entity<Assignment>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne(a => a.Ticket)
                  .WithMany(t => t.Assignments)
                  .HasForeignKey(a => a.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Technician)
                  .WithMany(tp => tp.Assignments)
                  .HasForeignKey(a => a.TechnicianProfileId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<NotificationHistory>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Title).HasMaxLength(200);
            entity.Property(n => n.EventType).HasMaxLength(50);
        });
    }
}
