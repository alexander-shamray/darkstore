using DarkStore.Domain.Couriers;
using DarkStore.Domain.Deliveries;
using DarkStore.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DarkStore.Application.Common.Interfaces;

/// <summary>
/// Abstraction over AppDbContext (Azure SQL — business data).
/// Defined in Application so handlers stay decoupled from EF Core SqlServer provider.
/// Implemented by DarkStore.Infrastructure.Persistence.AppDbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<Order>     Orders    { get; }
    DbSet<OrderItem> OrderItems { get; }
    DbSet<Courier>   Couriers  { get; }
    DbSet<Delivery>  Deliveries { get; }

    /// <summary>Persist all tracked changes to Azure SQL.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>EF Core Database facade — used for transactions and raw SQL.</summary>
    DatabaseFacade Database { get; }
}

