using DarkStore.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations.PersonalData;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.City).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Street).IsRequired().HasMaxLength(200);
        builder.Property(a => a.Building).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Apartment).HasMaxLength(50);
        builder.Property(a => a.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(a => a.Longitude).HasColumnType("decimal(9,6)");
    }
}

