using DarkStore.Application.Common.Interfaces;
using DarkStore.Domain.Couriers;
using DarkStore.Domain.Deliveries;
using DarkStore.Domain.Orders;
using Microsoft.EntityFrameworkCore;

namespace DarkStore.Infrastructure.Persistence;

/// <summary>
/// Azure SQL DbContext — business data only.
/// NO PII (no FullName, Phone, Email, IIN).
/// Only UserId / CourierId GUIDs cross the database boundary.
///
/// Implements <see cref="IAppDbContext"/> so Application layer handlers receive the
/// abstraction and stay testable without a real SQL Server connection.
/// </summary>
public class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Courier> Couriers => Set<Courier>();
    public DbSet<Delivery> Deliveries => Set<Delivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // FIX 1.4: DB-backed sequence for OrderNumber — eliminates Random.Next() collision risk.
        // Format: DS-2026-000001. Monotonically increasing, globally unique.
        modelBuilder.HasSequence<int>("OrderNumberSeq", schema: "dbo")
            .StartsAt(1)
            .IncrementsBy(1);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly,
            t => t.Namespace?.Contains("PersonalData") == false);
    }
}


