namespace DarkStore.Domain.Orders;

/// <summary>
/// Value object passed into Order.Create() carrying the address snapshot fields.
/// Populated from the Addresses table in KZ Local DB before crossing into domain layer.
/// Keeps the domain model free of any cross-DB dependency.
/// </summary>
public sealed record AddressSnapshot(
    string Street,
    string Building,
    string? Apartment,
    decimal Latitude,
    decimal Longitude,
    string DisplayLabel);

