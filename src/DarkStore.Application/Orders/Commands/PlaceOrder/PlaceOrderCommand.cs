using MediatR;

namespace DarkStore.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Places a new order for the authenticated customer.
/// Address fields are snapshotted from KZ Local DB by the caller before dispatching this command.
/// </summary>
/// <param name="UserId">Authenticated customer's GUID from JWT. Required.</param>
/// <param name="Street">Delivery street — snapshotted from customer's address.</param>
/// <param name="Building">Building number.</param>
/// <param name="Apartment">Apartment / office number (optional).</param>
/// <param name="Latitude">Delivery coordinates.</param>
/// <param name="Longitude">Delivery coordinates.</param>
/// <param name="DisplayLabel">Full human-readable label, e.g. "ул. Сатпаева, 7, кв. 12".</param>
/// <param name="Items">One or more order lines. Must not be empty.</param>
/// <param name="LoyaltyPointsToRedeem">Optional loyalty points to deduct from total.</param>
public sealed record PlaceOrderCommand(
    Guid UserId,
    string Street,
    string Building,
    string? Apartment,
    decimal Latitude,
    decimal Longitude,
    string DisplayLabel,
    IReadOnlyList<PlaceOrderItem> Items,
    int LoyaltyPointsToRedeem = 0
) : IRequest<PlaceOrderResponse>;

/// <param name="ProductId">SKU / product identifier.</param>
/// <param name="Quantity">Number of units. Must be ≥ 1.</param>
/// <param name="UnitPrice">Price per unit at time of order (snapshotted).</param>
public sealed record PlaceOrderItem(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice
);

