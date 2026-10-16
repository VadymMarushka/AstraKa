using AstraKa.Domain.Entities;
using AstraKa.Infrastructure.Persistence.Lookups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AstraKa.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.Property(u => u.ExternalId)
            .HasMaxLength(200)
            .IsRequired();
        builder.HasIndex(u => u.ExternalId).IsUnique();

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Stored as a tinyint FK to UserRoles, not a free-text string - see UserRoleLookup.
        builder.Property(u => u.Role)
            .HasConversion<byte>()
            .HasColumnName("RoleId")
            .IsRequired();

        builder.HasOne<UserRoleLookup>()
            .WithMany()
            .HasForeignKey(u => u.Role)
            .HasPrincipalKey(l => l.Id)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Wanderer / Starshepherd: Cascade (per docs/domain-model.md delete behaviors).
        builder.HasOne(u => u.Wanderer)
            .WithOne(w => w.User)
            .HasForeignKey<Wanderer>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(u => u.Starshepherd)
            .WithOne(s => s.User)
            .HasForeignKey<Starshepherd>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
