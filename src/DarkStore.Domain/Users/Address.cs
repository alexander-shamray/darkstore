using DarkStore.Domain.Orders;

namespace DarkStore.Domain.Users;

/// <summary>
/// Delivery address — stored in KZ Local DB (PersonalDataDbContext).
/// Used as the source when snapshotting into Order.DeliveryStreet/Building/etc.
/// </summary>
public class Address
{
    private Address() { } // EF Core

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string Street { get; private init; } = string.Empty;
    public string Building { get; private init; } = string.Empty;
    public string? Apartment { get; private init; }
    public decimal Latitude { get; private init; }
    public decimal Longitude { get; private init; }
    public bool IsDefault { get; private set; }

    public AddressSnapshot ToSnapshot()
    {
        string label = string.IsNullOrWhiteSpace(Apartment)
            ? $"{Street}, {Building}"
            : $"{Street}, {Building}, кв. {Apartment}";
        return new AddressSnapshot(Street, Building, Apartment, Latitude, Longitude, label);
    }

    public static Address Create(Guid userId, string city, string street, string building,
        string? apartment, decimal latitude, decimal longitude, bool isDefault = false)
    {
        return new Address
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            City = city,
            Street = street,
            Building = building,
            Apartment = apartment,
            Latitude = latitude,
            Longitude = longitude,
            IsDefault = isDefault
        };
    }
}


