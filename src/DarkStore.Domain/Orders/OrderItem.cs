namespace DarkStore.Domain.Orders;

public class OrderItem : Common.BaseEntity
{
    private OrderItem() { } // EF Core

    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    /// <summary>Price snapshot at the time of ordering — immune to future price changes.</summary>
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice { get; private set; }

    public static OrderItem Create(Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);

        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new OrderItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TotalPrice = quantity * unitPrice,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}

