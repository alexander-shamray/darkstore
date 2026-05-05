using MediatR;

namespace DarkStore.Application.Orders.Queries.GetOrderById;

/// <summary>Returns a single order by its primary key.</summary>
/// <param name="OrderId">Primary key of the order.</param>
/// <param name="RequestingUserId">
/// UserId from JWT. Customers may only see their own orders;
/// Admin/Picker roles bypass this check (enforced in the handler).
/// </param>
/// <param name="IsAdmin">When <c>true</c>, the ownership check is skipped.</param>
public sealed record GetOrderByIdQuery(
    Guid OrderId,
    Guid RequestingUserId,
    bool IsAdmin = false
) : IRequest<GetOrderByIdResponse?>;

