using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class HearingConfiguration : IEntityTypeConfiguration<Hearing>
{
    public void Configure(EntityTypeBuilder<Hearing> builder)
    {
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Status)
            .HasConversion<string>();

        builder.Property(h => h.Participants)
            .HasColumnType("jsonb");

        builder.HasOne(h => h.Case)
            .WithMany(c => c.Hearings)
            .HasForeignKey(h => h.CaseId);
    }
}
