namespace DarkStore.Domain.Users;

/// <summary>
/// Application user — stored in KZ Local DB (PersonalDataDbContext).
/// Contains PII: FullName, PhoneNumber, Email — all governed by Kazakhstan Law #94-V.
/// Azure SQL stores only UserId (GUID), never the PII fields.
/// </summary>
public class User
{
    private User() { } // EF Core

    public Guid Id { get; private set; }
    public string PhoneNumber { get; private set; } = string.Empty; // UNIQUE INDEX
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? ReferralSource { get; private set; }
    public bool Corporate { get; private set; }
    public string? CompanyName { get; private set; }
    public string? Bin { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsDeleted { get; private set; }

    public static User Create(string phoneNumber, string fullName, string? email = null, string? referralSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phoneNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);

        return new User
        {
            Id = Guid.NewGuid(),
            PhoneNumber = phoneNumber,
            FullName = fullName,
            Email = email,
            ReferralSource = referralSource,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void MakeCorporate(string companyName, string bin)
    {
        Corporate = true;
        CompanyName = companyName;
        Bin = bin;
    }

    public void SoftDelete() => IsDeleted = true;
}

