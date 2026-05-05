using DarkStore.Domain.Orders;
using DarkStore.Domain.Users;

namespace DarkStore.UnitTests.Users;

public class AddressTests
{
    private static readonly Guid _userId = Guid.NewGuid();

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_SetsAllProperties()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Сатпаева", "7", "12",
            53.2176m, 63.6354m, isDefault: true);

        address.Id.Should().NotBeEmpty();
        address.UserId.Should().Be(_userId);
        address.City.Should().Be("Костанай");
        address.Street.Should().Be("ул. Сатпаева");
        address.Building.Should().Be("7");
        address.Apartment.Should().Be("12");
        address.Latitude.Should().Be(53.2176m);
        address.Longitude.Should().Be(63.6354m);
        address.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Create_WithNullApartment_IsAllowed()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Ленина", "1",
            null, 53.2m, 63.6m);

        address.Apartment.Should().BeNull();
    }

    [Fact]
    public void Create_IsDefault_DefaultsToFalse()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Ленина", "1",
            null, 53.2m, 63.6m);

        address.IsDefault.Should().BeFalse();
    }

    // ── ToSnapshot ────────────────────────────────────────────────────────────

    [Fact]
    public void ToSnapshot_WithApartment_BuildsCorrectDisplayLabel()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Сатпаева", "7", "12",
            53.2176m, 63.6354m);

        AddressSnapshot snapshot = address.ToSnapshot();

        snapshot.Street.Should().Be("ул. Сатпаева");
        snapshot.Building.Should().Be("7");
        snapshot.Apartment.Should().Be("12");
        snapshot.Latitude.Should().Be(53.2176m);
        snapshot.Longitude.Should().Be(63.6354m);
        snapshot.DisplayLabel.Should().Be("ул. Сатпаева, 7, кв. 12");
    }

    [Fact]
    public void ToSnapshot_WithoutApartment_SkipsApartmentSegmentInLabel()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Сатпаева", "7",
            null, 53.2176m, 63.6354m);

        AddressSnapshot snapshot = address.ToSnapshot();

        snapshot.Apartment.Should().BeNull();
        snapshot.DisplayLabel.Should().Be("ул. Сатпаева, 7");
    }

    [Fact]
    public void ToSnapshot_CanBeUsedAsOrderInput()
    {
        var address = Address.Create(_userId, "Костанай", "ул. Сатпаева", "7", "12",
            53.2176m, 63.6354m);

        AddressSnapshot snapshot = address.ToSnapshot();

        // Verify the snapshot record is correctly populated for Order.Create
        snapshot.Should().NotBeNull();
        snapshot.Street.Should().NotBeNullOrWhiteSpace();
        snapshot.Building.Should().NotBeNullOrWhiteSpace();
        snapshot.DisplayLabel.Should().NotBeNullOrWhiteSpace();
    }
}
