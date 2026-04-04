using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class TimelineEventConfiguration : IEntityTypeConfiguration<TimelineEvent>
{
    public void Configure(EntityTypeBuilder<TimelineEvent> builder)
    {
        builder.HasKey(te => te.Id);

        builder.HasOne(te => te.Case)
            .WithMany(c => c.TimelineEvents)
            .HasForeignKey(te => te.CaseId);
    }
}
