namespace DarkStore.Domain.Couriers;

/// <summary>
/// Courier PII — stored in KZ Local DB (PersonalDataDbContext).
/// Kazakhstan Law #94-V: FullName, Phone, IIN are personal data of RK citizens
/// and MUST be stored on servers physically located in Kazakhstan.
///
/// Id is the same GUID as Courier.Id in Azure SQL — cross-DB link by convention, no FK.
/// </summary>
public class CourierProfile
{
    private CourierProfile() { } // EF Core

    /// <summary>Same GUID as Courier.Id in AppDbContext (Azure SQL). Not a FK — cross-DB link.</summary>
    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;

    /// <summary>Individual Identification Number (ИИН) — Kazakhstan tax ID.</summary>
    public string Iin { get; private set; } = string.Empty;

    public static CourierProfile Create(Guid courierId, string fullName, string phone, string iin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        ArgumentException.ThrowIfNullOrWhiteSpace(iin);

        return new CourierProfile
        {
            Id = courierId,
            FullName = fullName,
            Phone = phone,
            Iin = iin
        };
    }

    public void Update(string fullName, string phone, string iin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(phone);
        FullName = fullName;
        Phone = phone;
        Iin = iin;
    }
}

