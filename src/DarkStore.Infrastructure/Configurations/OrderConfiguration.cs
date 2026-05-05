using DarkStore.Domain.Deliveries;
using DarkStore.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for Order (Azure SQL / AppDbContext).
///
/// FIX 1.2 — No AddressId FK column. Address fields are a snapshot (value object columns).
/// FIX 1.3 — All DateTimeOffset columns stored as datetimeoffset(7) in SQL Server.
/// FIX 1.4 — OrderNumber uses SQL Server SEQUENCE "OrderNumberSeq".
///            UNIQUE INDEX prevents any collisions regardless of concurrency.
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        // ── FIX 1.4: DB-backed sequence — no more Random.Next() ──────────────
        // The sequence is defined once in AppDbContext.OnModelCreating (see bottom of this file).
        // SQL Server generates the value on INSERT via the default value expression.
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(30)
            .HasDefaultValueSql(
                "'DS-' + CAST(YEAR(GETUTCDATE()) AS VARCHAR(4)) + '-' + FORMAT(NEXT VALUE FOR dbo.OrderNumberSeq, 'D6')");

        // UNIQUE INDEX — enforces no collisions even if default value logic has edge cases
        builder.HasIndex(o => o.OrderNumber)
            .IsUnique()
            .HasDatabaseName("UX_Orders_OrderNumber");

        // ── FIX 1.2: No AddressId FK — address is stored as snapshot columns ─
        // (There is no AddressId property on Order at all — verified by domain model)
        builder.Property(o => o.DeliveryStreet).IsRequired().HasMaxLength(200);
        builder.Property(o => o.DeliveryBuilding).IsRequired().HasMaxLength(50);
        builder.Property(o => o.DeliveryApartment).HasMaxLength(50);
        builder.Property(o => o.DeliveryLatitude).HasColumnType("decimal(9,6)");
        builder.Property(o => o.DeliveryLongitude).HasColumnType("decimal(9,6)");
        builder.Property(o => o.DeliveryDisplayLabel).IsRequired().HasMaxLength(300);
        builder.Property(o => o.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(o => o.UpdatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(o => o.EstimatedDelivery).HasColumnType("datetimeoffset(7)");
        builder.Property(o => o.ActualDeliveryAt).HasColumnType("datetimeoffset(7)");

        // ── Amounts ──────────────────────────────────────────────────────────
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(o => o.CalculatedDeliveryFee).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(o => o.ActualDeliveryFee).HasColumnType("decimal(18,2)").IsRequired();

        // ── Indexes ──────────────────────────────────────────────────────────
        builder.HasIndex(o => o.UserId).HasDatabaseName("IX_Orders_UserId");
        builder.HasIndex(o => new { o.Status, o.CreatedAt }).HasDatabaseName("IX_Orders_Status_CreatedAt");
        builder.HasIndex(o => o.CreatedAt).HasDatabaseName("IX_Orders_CreatedAt");

        // ── Navigations ──────────────────────────────────────────────────────
        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.Delivery)
            .WithOne()
            .HasForeignKey<Delivery>(d => d.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}



