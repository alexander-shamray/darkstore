using DarkStore.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.ProductId).IsRequired();
        builder.Property(oi => oi.Quantity).IsRequired();
        builder.Property(oi => oi.UnitPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(oi => oi.TotalPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(oi => oi.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();
        builder.Property(oi => oi.UpdatedAt).HasColumnType("datetimeoffset(7)").IsRequired();

        builder.HasIndex(oi => oi.OrderId).HasDatabaseName("IX_OrderItems_OrderId");
    }
}

