using AstraKa.Domain.Enums;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleLookupConfiguration : IEntityTypeConfiguration<UserRoleLookup>
{
    public void Configure(EntityTypeBuilder<UserRoleLookup> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(l => l.Id);

        // Fixed, enum-driven ids - not DB-generated.
        builder.Property(l => l.Id)
            .HasConversion<byte>()
            .ValueGeneratedNever();

        builder.Property(l => l.Name)
            .HasMaxLength(20)
            .IsRequired();
        builder.HasIndex(l => l.Name).IsUnique();

        builder.HasData(
            new UserRoleLookup { Id = UserRole.Wanderer, Name = nameof(UserRole.Wanderer) },
            new UserRoleLookup { Id = UserRole.Starshepherd, Name = nameof(UserRole.Starshepherd) },
            new UserRoleLookup { Id = UserRole.Oracle, Name = nameof(UserRole.Oracle) });
    }
}
