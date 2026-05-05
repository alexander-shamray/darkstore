namespace DarkStore.Application.Orders.Queries.GetOrderById;

public sealed record GetOrderByIdResponse(
    Guid OrderId,
    string OrderNumber,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    decimal DeliveryFee,
    string DeliveryDisplayLabel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EstimatedDelivery,
    DateTimeOffset? ActualDeliveryAt,
    IReadOnlyList<GetOrderItemResponse> Items
);

public sealed record GetOrderItemResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal TotalPrice
);

