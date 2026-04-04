using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Key).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.Key).IsUnique();
        builder.Property(s => s.Value).IsRequired().HasMaxLength(1000);
        builder.Property(s => s.Category).IsRequired().HasMaxLength(50);
        builder.Property(s => s.Description).HasMaxLength(500);
    }
}
