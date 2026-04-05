using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CaseNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.CaseNumber)
            .IsUnique();

        builder.Property(c => c.Type)
            .HasConversion<string>();

        builder.Property(c => c.Status)
            .HasConversion<string>();

        builder.Property(c => c.Priority)
            .HasConversion<string>();

        builder.HasOne(c => c.AssignedTo)
            .WithMany()
            .HasForeignKey(c => c.AssignedToId)
            .IsRequired(false);

        builder.HasMany(c => c.Documents)
            .WithOne(d => d.Case)
            .HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Notes)
            .WithOne(n => n.Case)
            .HasForeignKey(n => n.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Hearings)
            .WithOne(h => h.Case)
            .HasForeignKey(h => h.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.TimelineEvents)
            .WithOne(te => te.Case)
            .HasForeignKey(te => te.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Reports)
            .WithOne(r => r.Case)
            .HasForeignKey(r => r.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Verdict).HasMaxLength(2000);
        builder.Property(c => c.Recommendation).HasMaxLength(2000);
        builder.Property(c => c.ForwardedToRole).HasMaxLength(50);
    }
}
