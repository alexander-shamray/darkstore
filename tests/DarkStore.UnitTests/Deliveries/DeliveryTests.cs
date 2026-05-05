using DarkStore.Domain.Deliveries;

namespace DarkStore.UnitTests.Deliveries;

public class DeliveryTests
{
    private static Delivery CreateDelivery() =>
        Delivery.Create(Guid.NewGuid(), createdBy: "system");

    private static readonly Guid _courierId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsInitialStatusToPending()
    {
        Delivery delivery = CreateDelivery();

        delivery.Status.Should().Be(DeliveryStatus.Pending);
        delivery.CourierId.Should().BeNull();
        delivery.StartedAt.Should().BeNull();
        delivery.CompletedAt.Should().BeNull();
        delivery.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_SetsOrderId()
    {
        var orderId = Guid.NewGuid();
        var delivery = Delivery.Create(orderId, "sys");

        delivery.OrderId.Should().Be(orderId);
    }

    // ── AssignCourier ─────────────────────────────────────────────────────────

    [Fact]
    public void AssignCourier_FromPending_SetsStatusAndCourierId()
    {
        Delivery delivery = CreateDelivery();

        delivery.AssignCourier(_courierId, "admin");

        delivery.Status.Should().Be(DeliveryStatus.Assigned);
        delivery.CourierId.Should().Be(_courierId);
    }

    [Fact]
    public void AssignCourier_FromNonPending_ThrowsInvalidOperationException()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");

        Action act = () => delivery.AssignCourier(Guid.NewGuid(), "admin");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_FromAssigned_SetsStatusAndStartedAt()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");
        DateTimeOffset before = DateTimeOffset.UtcNow;

        delivery.Start("courier");

        delivery.Status.Should().Be(DeliveryStatus.InProgress);
        delivery.StartedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void Start_FromPending_ThrowsInvalidOperationException()
    {
        Delivery delivery = CreateDelivery();

        Action act = () => delivery.Start("courier");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_FromInProgress_SetsStatusAndCompletedAt()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");
        delivery.Start("courier");
        DateTimeOffset before = DateTimeOffset.UtcNow;

        delivery.Complete("courier");

        delivery.Status.Should().Be(DeliveryStatus.Completed);
        delivery.CompletedAt.Should().NotBeNull().And.BeOnOrAfter(before);
    }

    [Fact]
    public void Complete_FromAssigned_ThrowsInvalidOperationException()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");

        Action act = () => delivery.Complete("courier");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── Fail ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fail_FromPendingOrAssigned_SetsStatusToFailed(bool assignFirst)
    {
        Delivery delivery = CreateDelivery();
        if (assignFirst)
        {
            delivery.AssignCourier(_courierId, "admin");
        }

        delivery.Fail("admin");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
    }

    [Fact]
    public void Fail_FromInProgress_SetsStatusToFailed()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");
        delivery.Start("courier");

        delivery.Fail("admin");

        delivery.Status.Should().Be(DeliveryStatus.Failed);
    }

    [Fact]
    public void Fail_FromCompleted_ThrowsInvalidOperationException()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");
        delivery.Start("courier");
        delivery.Complete("courier");

        Action act = () => delivery.Fail("admin");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Fail_FromFailed_ThrowsInvalidOperationException()
    {
        Delivery delivery = CreateDelivery();
        delivery.Fail("admin");

        Action act = () => delivery.Fail("admin");

        act.Should().Throw<InvalidOperationException>();
    }

    // ── UpdateLocation ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLocation_SetsCoordinates()
    {
        Delivery delivery = CreateDelivery();

        delivery.UpdateLocation(47.1075m, 51.9194m);

        delivery.CurrentLatitude.Should().Be(47.1075m);
        delivery.CurrentLongitude.Should().Be(51.9194m);
    }

    [Fact]
    public void UpdateLocation_CanBeCalledMultipleTimes()
    {
        Delivery delivery = CreateDelivery();
        delivery.UpdateLocation(47.1m, 51.9m);
        delivery.UpdateLocation(47.2m, 51.8m);

        delivery.CurrentLatitude.Should().Be(47.2m);
        delivery.CurrentLongitude.Should().Be(51.8m);
    }

    // ── Terminal states ───────────────────────────────────────────────────────

    [Fact]
    public void Completed_IsTerminal_CannotTransitionFurther()
    {
        Delivery delivery = CreateDelivery();
        delivery.AssignCourier(_courierId, "admin");
        delivery.Start("courier");
        delivery.Complete("courier");

        Action act = () => delivery.Complete("courier");

        act.Should().Throw<InvalidOperationException>();
    }
}


