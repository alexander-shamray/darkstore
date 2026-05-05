using DarkStore.Application.Common.Interfaces;
using DarkStore.Domain.Orders;
using MediatR;

namespace DarkStore.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Creates a new <see cref="Order"/> and persists it to Azure SQL.
///
/// Delivery fee rule: free shipping when total ≥ ₸10 000 (applied by Order domain logic).
/// Loyalty points discount applied last, after all items are added.
/// </summary>
public sealed class PlaceOrderCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<PlaceOrderCommand, PlaceOrderResponse>
{
    // Flat delivery fee — replace with real pricing service in Q1.
    private const decimal _flatDeliveryFee = 490m;

    public async Task<PlaceOrderResponse> Handle(
        PlaceOrderCommand request,
        CancellationToken cancellationToken)
    {
        var addressSnapshot = new AddressSnapshot(
            request.Street,
            request.Building,
            request.Apartment,
            request.Latitude,
            request.Longitude,
            request.DisplayLabel);

        var order = Order.Create(
            userId: request.UserId,
            addressSnapshot: addressSnapshot,
            calculatedDeliveryFee: _flatDeliveryFee,
            createdBy: currentUser.UserIdOrAnonymous);

        foreach (PlaceOrderItem item in request.Items)
        {
            order.AddItem(item.ProductId, item.Quantity, item.UnitPrice, currentUser.UserIdOrAnonymous);
        }

        if (request.LoyaltyPointsToRedeem > 0)
        {
            order.ApplyLoyaltyDiscount(request.LoyaltyPointsToRedeem, currentUser.UserIdOrAnonymous);
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResponse(
            OrderId: order.Id,
            OrderNumber: order.OrderNumber,
            Status: order.Status.ToString(),
            TotalAmount: order.TotalAmount,
            DeliveryFee: order.ActualDeliveryFee,
            CreatedAt: order.CreatedAt);
    }
}

