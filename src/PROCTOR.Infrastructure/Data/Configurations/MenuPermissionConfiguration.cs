using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class MenuPermissionConfiguration : IEntityTypeConfiguration<MenuPermission>
{
    public void Configure(EntityTypeBuilder<MenuPermission> builder)
    {
        builder.HasKey(mp => mp.Id);

        builder.Property(mp => mp.MenuKey)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(mp => new { mp.RoleId, mp.MenuKey })
            .IsUnique();

        builder.HasOne(mp => mp.Role)
            .WithMany(r => r.MenuPermissions)
            .HasForeignKey(mp => mp.RoleId);
    }
}
