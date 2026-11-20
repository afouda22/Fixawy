using Fixawyy.Models;
using Microsoft.EntityFrameworkCore;

namespace Fixawyy.Context;

/// <summary>
/// Fluent API configuration for the Booking &amp; Review entities (Teammate 2).
/// Kept in its own file so AppDbContext only needs one extra line.
/// No existing entity is changed: relationships are one-directional from the new entities.
/// </summary>
public static class BookingModelConfiguration
{
    public static void Apply(ModelBuilder builder)
    {
        builder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
            entity.Property(b => b.Notes).HasMaxLength(500);
            entity.Property(b => b.RowVersion).IsRowVersion();

            entity.HasOne(b => b.Customer)
                  .WithMany()
                  .HasForeignKey(b => b.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Technician)
                  .WithMany()
                  .HasForeignKey(b => b.TechnicianId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(b => new { b.TechnicianId, b.BookingDate });
            entity.HasIndex(b => b.CustomerId);

            // Safety net against double booking: two non-cancelled bookings can never start at the same
            // moment for the same technician. (Partial overlaps are blocked by BookingService under a lock.)
            // Status 3 = Cancelled.
            entity.HasIndex(b => new { b.TechnicianId, b.BookingDate, b.StartTime })
                  .IsUnique()
                  .HasFilter("[Status] <> 3");
        });

        builder.Entity<BookingItem>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.ServiceName).HasMaxLength(100).IsRequired();
            entity.Property(i => i.Price).HasColumnType("decimal(18,2)");

            entity.HasOne(i => i.Booking)
                  .WithMany(b => b.Items)
                  .HasForeignKey(i => i.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Specialty)
                  .WithMany()
                  .HasForeignKey(i => i.SpecialtyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Review>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Comment).HasMaxLength(1000);

            entity.HasOne(r => r.Booking)
                  .WithOne(b => b.Review)
                  .HasForeignKey<Review>(r => r.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Customer)
                  .WithMany()
                  .HasForeignKey(r => r.CustomerId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(r => r.Technician)
                  .WithMany()
                  .HasForeignKey(r => r.TechnicianId)
                  .OnDelete(DeleteBehavior.Restrict);

            // One review per booking.
            entity.HasIndex(r => r.BookingId).IsUnique();
            entity.HasIndex(r => r.TechnicianId);
        });
    }
}
