using DarkStore.Domain.Couriers;
using DarkStore.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DarkStore.Infrastructure.Persistence;

/// <summary>
/// KZ Local DB DbContext — personal data only (Kazakhstan Law #94-V).
/// This DbContext MUST connect to a server physically located in Kazakhstan.
/// Never add business entities (Orders, Products, Inventory, etc.) here.
/// </summary>
public class PersonalDataDbContext(DbContextOptions<PersonalDataDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<CourierProfile> CourierProfiles => Set<CourierProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PersonalDataDbContext).Assembly,
            t => t.Namespace?.Contains("PersonalData") == true);
    }
}

