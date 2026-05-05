using DarkStore.Domain.Orders;

namespace DarkStore.UnitTests.Orders;

public class OrderItemTests
{
    private static readonly Guid _orderId = Guid.NewGuid();
    private static readonly Guid _productId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArguments_SetsAllProperties()
    {
        var item = OrderItem.Create(_orderId, _productId, quantity: 3, unitPrice: 500m);

        item.OrderId.Should().Be(_orderId);
        item.ProductId.Should().Be(_productId);
        item.Quantity.Should().Be(3);
        item.UnitPrice.Should().Be(500m);
        item.TotalPrice.Should().Be(1500m);
        item.Id.Should().NotBeEmpty();
        item.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithZeroOrNegativeQuantity_ThrowsArgumentOutOfRangeException(int quantity)
    {
        Func<OrderItem> act = () => OrderItem.Create(_orderId, _productId, quantity, unitPrice: 100m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("quantity");
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ThrowsArgumentOutOfRangeException()
    {
        Func<OrderItem> act = () => OrderItem.Create(_orderId, _productId, quantity: 1, unitPrice: -1m);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("unitPrice");
    }

    [Fact]
    public void Create_WithZeroUnitPrice_IsAllowed()
    {
        // Free product / promotional item
        var item = OrderItem.Create(_orderId, _productId, quantity: 2, unitPrice: 0m);

        item.TotalPrice.Should().Be(0m);
    }

    [Fact]
    public void Create_TotalPrice_IsQuantityTimesUnitPrice()
    {
        var item = OrderItem.Create(_orderId, _productId, quantity: 7, unitPrice: 150m);

        item.TotalPrice.Should().Be(1050m);
    }
}


