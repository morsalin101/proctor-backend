using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Content)
            .IsRequired();

        builder.Property(r => r.CreatedByName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(r => r.Case)
            .WithMany(c => c.Reports)
            .HasForeignKey(r => r.CaseId);
    }
}
