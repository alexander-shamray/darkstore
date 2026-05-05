namespace DarkStore.Application.Orders.Commands.PlaceOrder;

/// <summary>Response DTO returned after a successful <see cref="PlaceOrderCommand"/>.</summary>
public sealed record PlaceOrderResponse(
    Guid OrderId,
    string OrderNumber,
    string Status,
    decimal TotalAmount,
    decimal DeliveryFee,
    DateTimeOffset CreatedAt
);

