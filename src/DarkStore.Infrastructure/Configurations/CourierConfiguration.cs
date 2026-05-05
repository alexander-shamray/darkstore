using DarkStore.Domain.Couriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Courier (Azure SQL / AppDbContext).
///
/// FIX 1.1 — No FullName / Phone columns here. Those live in CourierProfileConfiguration
///            in PersonalDataDbContext (KZ Local DB). The Courier row in Azure SQL
///            contains only business data: IsActive, VehicleType, Rating, coordinates.
/// </summary>
public class CourierConfiguration : IEntityTypeConfiguration<Courier>
{
    public void Configure(EntityTypeBuilder<Courier> builder)
    {
        builder.ToTable("Couriers");
        builder.HasKey(c => c.Id);

        // ── FIX 1.1: ABSENT columns (enforced by domain model + this config) ─
        // FullName → CourierProfiles table in KZ Local DB
        // Phone    → CourierProfiles table in KZ Local DB
        // IIN      → CourierProfiles table in KZ Local DB
        // The Courier.Id GUID is shared with CourierProfile.Id — no FK possible across DBs.

        builder.Property(c => c.Rating).HasColumnType("decimal(3,2)");
        builder.Property(c => c.CurrentLatitude).HasColumnType("decimal(9,6)");
        builder.Property(c => c.CurrentLongitude).HasColumnType("decimal(9,6)");
        builder.Property(c => c.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnType("datetimeoffset(7)").IsRequired();

        builder.HasMany(c => c.Deliveries)
            .WithOne()
            .HasForeignKey(d => d.CourierId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

