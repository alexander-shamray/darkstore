namespace DarkStore.Domain.Common;

/// <summary>
/// Base class for all domain entities.
/// Uses DateTimeOffset (not DateTime) to correctly handle UTC+6 Kazakhstan timezone.
/// All timestamps are stored as UTC and converted to Asia/Almaty only at the presentation layer.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; protected init; }

    /// <summary>DateTimeOffset — stores UTC offset, avoids timezone ambiguity for KZ UTC+6.</summary>
    public DateTimeOffset CreatedAt { get; protected init; }

    /// <summary>DateTimeOffset — stores UTC offset, avoids timezone ambiguity for KZ UTC+6.</summary>
    public DateTimeOffset UpdatedAt { get; protected set; }

    /// <summary>UserId or "system" for background jobs.</summary>
    public string CreatedBy { get; protected init; } = string.Empty;

    public string UpdatedBy { get; protected set; } = string.Empty;

    public bool IsDeleted { get; set; }
}

