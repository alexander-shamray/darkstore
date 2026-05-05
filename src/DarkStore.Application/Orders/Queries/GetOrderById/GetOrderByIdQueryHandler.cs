using DarkStore.Application.Common.Interfaces;
using DarkStore.Domain.Orders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DarkStore.Application.Orders.Queries.GetOrderById;

/// <summary>
/// Fetches a single order by ID using EF Core.
/// For bulk/reporting queries, replace with a Dapper-based IDbConnection read
/// (CQRS read side — 3.1 in the improvement plan, Q1).
/// Returns <c>null</c> when the order does not exist.
/// Returns <c>null</c> when a non-admin requests another user's order (no information leak).
/// </summary>
public sealed class GetOrderByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetOrderByIdQuery, GetOrderByIdResponse?>
{
    public async Task<GetOrderByIdResponse?> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        Order? order = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted, cancellationToken);

        if (order is null)
        {
            return null;
        }

        // Ownership check — customers see only their own orders.
        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
        {
            return null;
        }

        return new GetOrderByIdResponse(
            OrderId:              order.Id,
            OrderNumber:          order.OrderNumber,
            UserId:               order.UserId,
            Status:               order.Status.ToString(),
            TotalAmount:          order.TotalAmount,
            DeliveryFee:          order.ActualDeliveryFee,
            DeliveryDisplayLabel: order.DeliveryDisplayLabel,
            CreatedAt:            order.CreatedAt,
            EstimatedDelivery:    order.EstimatedDelivery,
            ActualDeliveryAt:     order.ActualDeliveryAt,
            Items: [.. order.Items.Select(i => new GetOrderItemResponse(i.ProductId, i.Quantity, i.UnitPrice, i.TotalPrice))]);
    }
}


