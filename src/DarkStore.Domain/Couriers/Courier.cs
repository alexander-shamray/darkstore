namespace DarkStore.Domain.Couriers;

/// <summary>
/// Courier business data — stored in Azure SQL (AppDbContext).
/// FullName and Phone are INTENTIONALLY ABSENT here: they are PII governed by
/// Kazakhstan Law #94-V and must reside in KZ Local DB (PersonalDataDbContext → CourierProfile).
/// Link between the two: CourierProfile.Id == Courier.Id (shared GUID, no FK).
/// </summary>
public class Courier : Common.BaseEntity
{
    private Courier() { } // EF Core

    public bool IsActive { get; private set; }
    public VehicleType VehicleType { get; private set; }
    public decimal Rating { get; private set; }
    public decimal? CurrentLatitude { get; private set; }
    public decimal? CurrentLongitude { get; private set; }

    public IReadOnlyCollection<Deliveries.Delivery> Deliveries => _deliveries.AsReadOnly();
    private readonly List<Deliveries.Delivery> _deliveries = [];

    public static Courier Create(Guid courierId, VehicleType vehicleType, string createdBy)
    {
        return new Courier
        {
            Id = courierId, // Must match CourierProfile.Id created in KZ Local DB
            IsActive = true,
            VehicleType = vehicleType,
            Rating = 5.0m,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate(string updatedBy)
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }
}

public enum VehicleType
{
    OnFoot = 0,
    Bicycle = 1,
    Motorcycle = 2,
    Car = 3
}

