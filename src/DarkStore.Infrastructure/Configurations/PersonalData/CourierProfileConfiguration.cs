using DarkStore.Domain.Couriers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations.PersonalData;

/// <summary>
/// EF Core configuration for CourierProfile (KZ Local DB / PersonalDataDbContext).
///
/// FIX 1.1 — FullName, Phone, IIN are stored HERE (KZ Local DB), not in Azure SQL Couriers table.
/// CourierProfile.Id = Courier.Id — shared GUID. No FK possible between databases.
/// </summary>
public class CourierProfileConfiguration : IEntityTypeConfiguration<CourierProfile>
{
    public void Configure(EntityTypeBuilder<CourierProfile> builder)
    {
        builder.ToTable("CourierProfiles");
        builder.HasKey(cp => cp.Id);

        // Id is not auto-generated — it's set explicitly to match Courier.Id in Azure SQL
        builder.Property(cp => cp.Id).ValueGeneratedNever();

        builder.Property(cp => cp.FullName).IsRequired().HasMaxLength(200);
        builder.Property(cp => cp.Phone).IsRequired().HasMaxLength(20);
        builder.Property(cp => cp.Iin).IsRequired().HasMaxLength(12);

        builder.HasIndex(cp => cp.Phone).HasDatabaseName("IX_CourierProfiles_Phone");
    }
}

