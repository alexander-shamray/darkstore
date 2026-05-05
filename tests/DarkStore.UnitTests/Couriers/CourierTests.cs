using DarkStore.Domain.Couriers;

namespace DarkStore.UnitTests.Couriers;

public class CourierTests
{
    private static Courier CreateCourier(VehicleType vehicleType = VehicleType.Bicycle) =>
        Courier.Create(Guid.NewGuid(), vehicleType, createdBy: "admin");

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsInitialState()
    {
        var courierId = Guid.NewGuid();
        var courier = Courier.Create(courierId, VehicleType.Motorcycle, "admin");

        courier.Id.Should().Be(courierId);
        courier.IsActive.Should().BeTrue();
        courier.VehicleType.Should().Be(VehicleType.Motorcycle);
        courier.Rating.Should().Be(5.0m);
        courier.CurrentLatitude.Should().BeNull();
        courier.CurrentLongitude.Should().BeNull();
        courier.Deliveries.Should().BeEmpty();
    }

    [Theory]
    [InlineData(VehicleType.OnFoot)]
    [InlineData(VehicleType.Bicycle)]
    [InlineData(VehicleType.Motorcycle)]
    [InlineData(VehicleType.Car)]
    public void Create_ForEveryVehicleType_Succeeds(VehicleType vehicleType)
    {
        var courier = Courier.Create(Guid.NewGuid(), vehicleType, "admin");

        courier.VehicleType.Should().Be(vehicleType);
    }

    [Fact]
    public void Create_SetsCreatedByAndUpdatedBy()
    {
        var courier = Courier.Create(Guid.NewGuid(), VehicleType.Car, "dispatcher");

        courier.CreatedBy.Should().Be("dispatcher");
        courier.UpdatedBy.Should().Be("dispatcher");
    }

    // ── UpdateLocation ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateLocation_SetsCoordinates()
    {
        Courier courier = CreateCourier();

        courier.UpdateLocation(47.1075m, 51.9194m);

        courier.CurrentLatitude.Should().Be(47.1075m);
        courier.CurrentLongitude.Should().Be(51.9194m);
    }

    [Fact]
    public void UpdateLocation_OverwritesPreviousCoordinates()
    {
        Courier courier = CreateCourier();
        courier.UpdateLocation(47.1m, 51.9m);

        courier.UpdateLocation(48.0m, 52.0m);

        courier.CurrentLatitude.Should().Be(48.0m);
        courier.CurrentLongitude.Should().Be(52.0m);
    }

    [Fact]
    public void UpdateLocation_UpdatesUpdatedAt()
    {
        Courier courier = CreateCourier();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        courier.UpdateLocation(47.1m, 51.9m);

        courier.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_SetsIsActiveToFalse()
    {
        Courier courier = CreateCourier();

        courier.Deactivate("admin");

        courier.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_UpdatesUpdatedByAndUpdatedAt()
    {
        Courier courier = CreateCourier();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        courier.Deactivate("supervisor");

        courier.UpdatedBy.Should().Be("supervisor");
        courier.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Deactivate_CalledTwice_StaysInactive()
    {
        Courier courier = CreateCourier();
        courier.Deactivate("admin");

        courier.Deactivate("admin");

        courier.IsActive.Should().BeFalse();
    }
}


