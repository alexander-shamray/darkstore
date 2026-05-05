using DarkStore.Domain.Orders;

namespace DarkStore.UnitTests.Orders;

public class OrderTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static AddressSnapshot DefaultAddress() => new(
        Street: "ул. Сатпаева",
        Building: "7",
        Apartment: "12",
        Latitude: 47.1075m,
        Longitude: 51.9194m,
        DisplayLabel: "ул. Сатпаева, 7, кв. 12");

    private static Order CreateOrder(decimal deliveryFee = 500m) =>
        Order.Create(Guid.NewGuid(), DefaultAddress(), deliveryFee, createdBy: "test-user");

    private static readonly Guid _productId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidInputs_SetsAllFields()
    {
        var userId = Guid.NewGuid();
        AddressSnapshot address = DefaultAddress();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        var order = Order.Create(userId, address, calculatedDeliveryFee: 300m, createdBy: "admin");

        order.UserId.Should().Be(userId);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(0m);
        order.CalculatedDeliveryFee.Should().Be(300m);
        order.ActualDeliveryFee.Should().Be(300m);
        order.LoyaltyPointsUsed.Should().Be(0);
        order.Id.Should().NotBeEmpty();
        order.CreatedAt.Should().BeOnOrAfter(before);
        order.CreatedBy.Should().Be("admin");
        order.Items.Should().BeEmpty();
    }

    [Fact]
    public void Create_SnapshotsAddressFields()
    {
        AddressSnapshot address = DefaultAddress();
        var order = Order.Create(Guid.NewGuid(), address, 0m, "sys");

        order.DeliveryStreet.Should().Be(address.Street);
        order.DeliveryBuilding.Should().Be(address.Building);
        order.DeliveryApartment.Should().Be(address.Apartment);
        order.DeliveryLatitude.Should().Be(address.Latitude);
        order.DeliveryLongitude.Should().Be(address.Longitude);
        order.DeliveryDisplayLabel.Should().Be(address.DisplayLabel);
    }

    [Fact]
    public void Create_WithNullAddressSnapshot_ThrowsArgumentNullException()
    {
        Func<Order> act = () => Order.Create(Guid.NewGuid(), null!, 300m, "sys");

        act.Should().Throw<ArgumentNullException>();
    }

    // ── AddItem ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ToPendingOrder_UpdatesTotalAmount()
    {
        Order order = CreateOrder(deliveryFee: 500m);

        order.AddItem(_productId, quantity: 2, unitPrice: 1000m, updatedBy: "user");

        order.TotalAmount.Should().Be(2000m);
        order.Items.Should().HaveCount(1);
    }

    [Fact]
    public void AddItem_MultipleItems_AccumulatesTotalAmount()
    {
        Order order = CreateOrder(deliveryFee: 500m);

        order.AddItem(Guid.NewGuid(), 1, 500m, "user");
        order.AddItem(Guid.NewGuid(), 3, 200m, "user");

        order.TotalAmount.Should().Be(1100m);
        order.Items.Should().HaveCount(2);
    }

    [Fact]
    public void AddItem_WhenTotalReaches10000_SetsActualDeliveryFeeToZero()
    {
        Order order = CreateOrder(deliveryFee: 500m);

        order.AddItem(Guid.NewGuid(), 1, 10_000m, "user");

        order.ActualDeliveryFee.Should().Be(0m);
    }

    [Fact]
    public void AddItem_WhenTotalBelow10000_KeepsCalculatedDeliveryFee()
    {
        Order order = CreateOrder(deliveryFee: 500m);

        order.AddItem(Guid.NewGuid(), 1, 9_999m, "user");

        order.ActualDeliveryFee.Should().Be(500m);
    }

    [Fact]
    public void AddItem_ToNonPendingOrder_ThrowsInvalidOperationException()
    {
        Order order = CreateOrder();
        order.TransitionStatus(OrderStatus.Confirmed, "admin");

        Action act = () => order.AddItem(_productId, 1, 100m, "user");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*non-pending*");
    }

    // ── TransitionStatus ─────────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Pending,        OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Pending,        OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Confirmed,      OrderStatus.Preparing)]
    [InlineData(OrderStatus.Confirmed,      OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Preparing,      OrderStatus.ReadyForPickup)]
    [InlineData(OrderStatus.Preparing,      OrderStatus.Cancelled)]
    [InlineData(OrderStatus.ReadyForPickup, OrderStatus.OnTheWay)]
    [InlineData(OrderStatus.ReadyForPickup, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.OnTheWay,       OrderStatus.Delivered)]
    [InlineData(OrderStatus.OnTheWay,       OrderStatus.Cancelled)]
    public void TransitionStatus_ValidTransitions_Succeed(OrderStatus from, OrderStatus to)
    {
        Order order = CreateOrder();
        AdvanceOrderTo(order, from);

        Action act = () => order.TransitionStatus(to, "admin");

        act.Should().NotThrow();
        order.Status.Should().Be(to);
    }

    [Theory]
    [InlineData(OrderStatus.Pending,   OrderStatus.Preparing)]
    [InlineData(OrderStatus.Pending,   OrderStatus.Delivered)]
    [InlineData(OrderStatus.Confirmed, OrderStatus.Delivered)]
    [InlineData(OrderStatus.Delivered, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Confirmed)]
    public void TransitionStatus_InvalidTransitions_ThrowsInvalidOperationException(OrderStatus from, OrderStatus to)
    {
        Order order = CreateOrder();
        AdvanceOrderTo(order, from);

        Action act = () => order.TransitionStatus(to, "admin");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TransitionStatus_ToDelivered_SetsActualDeliveryAt()
    {
        Order order = CreateOrder();
        AdvanceOrderTo(order, OrderStatus.OnTheWay);
        DateTimeOffset before = DateTimeOffset.UtcNow;

        order.TransitionStatus(OrderStatus.Delivered, "admin");

        order.ActualDeliveryAt.Should().NotBeNull();
        order.ActualDeliveryAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void TransitionStatus_ToNonDelivered_DoesNotSetActualDeliveryAt()
    {
        Order order = CreateOrder();

        order.TransitionStatus(OrderStatus.Confirmed, "admin");

        order.ActualDeliveryAt.Should().BeNull();
    }

    [Fact]
    public void TransitionStatus_UpdatesUpdatedByAndUpdatedAt()
    {
        Order order = CreateOrder();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        order.TransitionStatus(OrderStatus.Confirmed, "dispatcher");

        order.UpdatedBy.Should().Be("dispatcher");
        order.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // ── ApplyLoyaltyDiscount ─────────────────────────────────────────────────

    [Fact]
    public void ApplyLoyaltyDiscount_ReducesTotalAmount()
    {
        Order order = CreateOrder();
        order.AddItem(_productId, 1, 500m, "user");

        order.ApplyLoyaltyDiscount(points: 100, updatedBy: "user");

        order.TotalAmount.Should().Be(400m);
        order.LoyaltyPointsUsed.Should().Be(100);
    }

    [Fact]
    public void ApplyLoyaltyDiscount_CannotResultInNegativeTotal()
    {
        Order order = CreateOrder();
        order.AddItem(_productId, 1, 50m, "user");

        order.ApplyLoyaltyDiscount(points: 9999, updatedBy: "user");

        order.TotalAmount.Should().Be(0m);
    }

    // ── Terminal states ───────────────────────────────────────────────────────

    [Fact]
    public void TransitionStatus_FromDelivered_ThrowsInvalidOperationException()
    {
        Order order = CreateOrder();
        AdvanceOrderTo(order, OrderStatus.Delivered);

        Action act = () => order.TransitionStatus(OrderStatus.Cancelled, "admin");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void TransitionStatus_FromCancelled_ThrowsInvalidOperationException()
    {
        Order order = CreateOrder();
        order.TransitionStatus(OrderStatus.Cancelled, "admin");

        Action act = () => order.TransitionStatus(OrderStatus.Confirmed, "admin");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>Advances a freshly-created (Pending) order to the target status via valid transitions.</summary>
    private static void AdvanceOrderTo(Order order, OrderStatus target)
    {
        var path = new List<OrderStatus>
        {
            OrderStatus.Pending,
            OrderStatus.Confirmed,
            OrderStatus.Preparing,
            OrderStatus.ReadyForPickup,
            OrderStatus.OnTheWay,
            OrderStatus.Delivered
        };

        if (target == OrderStatus.Cancelled)
        {
            // Cancel is always valid from Pending — go straight there.
            order.TransitionStatus(OrderStatus.Cancelled, "system");
            return;
        }

        int startIdx = path.IndexOf(order.Status);
        int endIdx = path.IndexOf(target);

        for (int i = startIdx; i < endIdx; i++)
        {
            order.TransitionStatus(path[i + 1], "system");
        }
    }
}



