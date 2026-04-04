using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RoleName)
            .HasConversion<string>();

        builder.HasIndex(r => r.RoleName)
            .IsUnique();

        builder.Property(r => r.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasMany(r => r.MenuPermissions)
            .WithOne(mp => mp.Role)
            .HasForeignKey(mp => mp.RoleId);
    }
}
