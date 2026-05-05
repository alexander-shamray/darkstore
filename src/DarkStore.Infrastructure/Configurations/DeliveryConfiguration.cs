using DarkStore.Domain.Deliveries;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations;

public class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
    public void Configure(EntityTypeBuilder<Delivery> builder)
    {
        builder.ToTable("Deliveries");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.CurrentLatitude).HasColumnType("decimal(9,6)");
        builder.Property(d => d.CurrentLongitude).HasColumnType("decimal(9,6)");
        builder.Property(d => d.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(d => d.UpdatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(d => d.StartedAt).HasColumnType("datetimeoffset(7)");
        builder.Property(d => d.CompletedAt).HasColumnType("datetimeoffset(7)");

        builder.HasIndex(d => d.CourierId).HasDatabaseName("IX_Deliveries_CourierId");
        builder.HasIndex(d => d.OrderId).IsUnique().HasDatabaseName("UX_Deliveries_OrderId");
    }
}

