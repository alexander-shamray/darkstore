using DarkStore.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DarkStore.Infrastructure.Configurations.PersonalData;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).HasMaxLength(200);
        builder.Property(u => u.ReferralSource).HasMaxLength(100);
        builder.Property(u => u.CompanyName).HasMaxLength(200);
        builder.Property(u => u.Bin).HasMaxLength(12);
        builder.Property(u => u.CreatedAt).HasColumnType("datetimeoffset(7)").IsRequired();

        builder.HasIndex(u => u.PhoneNumber).IsUnique().HasDatabaseName("UX_Users_PhoneNumber");

        builder.HasMany<Address>()
            .WithOne()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

