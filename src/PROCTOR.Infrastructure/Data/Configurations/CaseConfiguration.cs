using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.HearingPersons)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<CaseHearingPerson>()
                    : JsonSerializer.Deserialize<List<CaseHearingPerson>>(v, (JsonSerializerOptions?)null) ?? new(),
                new ValueComparer<List<CaseHearingPerson>>(
                    (a, b) => JsonSerializer.Serialize(a, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(b, (JsonSerializerOptions?)null),
                    v => v == null ? 0 : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null).GetHashCode(),
                    v => JsonSerializer.Deserialize<List<CaseHearingPerson>>(JsonSerializer.Serialize(v, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new()))
            .HasColumnType("jsonb");

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

        builder.Property(c => c.SubmitterGender)
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

        builder.HasMany(c => c.Verifications)
            .WithOne(v => v.Case)
            .HasForeignKey(v => v.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Complainants)
            .WithOne(cc => cc.Case)
            .HasForeignKey(cc => cc.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.AccusedPersons)
            .WithOne(ca => ca.Case)
            .HasForeignKey(ca => ca.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.AdditionalInfos)
            .WithOne(ai => ai.Case)
            .HasForeignKey(ai => ai.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.Verdict).HasMaxLength(2000);
        builder.Property(c => c.Recommendation).HasMaxLength(2000);
        builder.Property(c => c.ForwardedToRole).HasMaxLength(50);
    }
}
