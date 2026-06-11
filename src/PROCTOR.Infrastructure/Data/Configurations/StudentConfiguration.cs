using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentId)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(s => s.StudentId).IsUnique();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Gender)
            .HasConversion<string>();

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);
    }
}
