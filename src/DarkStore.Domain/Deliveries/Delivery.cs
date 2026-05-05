namespace DarkStore.Domain.Deliveries;

public class Delivery : Common.BaseEntity
{
    private Delivery() { } // EF Core

    public Guid OrderId { get; private set; }
    public Guid? CourierId { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public decimal? CurrentLatitude { get; private set; }
    public decimal? CurrentLongitude { get; private set; }

    // ── Valid state-machine transitions ─────────────────────────────────────
    private static readonly IReadOnlyDictionary<DeliveryStatus, IReadOnlyCollection<DeliveryStatus>> _validTransitions =
        new Dictionary<DeliveryStatus, IReadOnlyCollection<DeliveryStatus>>
        {
            [DeliveryStatus.Pending]    = [DeliveryStatus.Assigned,    DeliveryStatus.Failed],
            [DeliveryStatus.Assigned]   = [DeliveryStatus.InProgress,  DeliveryStatus.Failed],
            [DeliveryStatus.InProgress] = [DeliveryStatus.Completed,   DeliveryStatus.Failed],
            [DeliveryStatus.Completed]  = [],
            [DeliveryStatus.Failed]     = []
        };

    private void ValidateTransition(DeliveryStatus next)
    {
        if (!_validTransitions.TryGetValue(Status, out IReadOnlyCollection<DeliveryStatus>? allowed) || !allowed.Contains(next))
        {
            throw new InvalidOperationException(
                $"Cannot transition delivery from {Status} to {next}. " +
                $"Valid: [{string.Join(", ", _validTransitions.GetValueOrDefault(Status) ?? [])}].");
        }
    }

    public static Delivery Create(Guid orderId, string createdBy)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Delivery
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            Status = DeliveryStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = createdBy,
            UpdatedBy = createdBy
        };
    }

    public void AssignCourier(Guid courierId, string updatedBy)
    {
        ValidateTransition(DeliveryStatus.Assigned);
        CourierId = courierId;
        Status = DeliveryStatus.Assigned;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Start(string updatedBy)
    {
        ValidateTransition(DeliveryStatus.InProgress);
        Status = DeliveryStatus.InProgress;
        StartedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Complete(string updatedBy)
    {
        ValidateTransition(DeliveryStatus.Completed);
        Status = DeliveryStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void Fail(string updatedBy)
    {
        ValidateTransition(DeliveryStatus.Failed);
        Status = DeliveryStatus.Failed;
        UpdatedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
    }

    public void UpdateLocation(decimal latitude, decimal longitude)
    {
        CurrentLatitude = latitude;
        CurrentLongitude = longitude;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

public enum DeliveryStatus
{
    Pending = 0,
    Assigned = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4
}

