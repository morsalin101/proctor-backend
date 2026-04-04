using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Content)
            .IsRequired();

        builder.HasOne(n => n.Case)
            .WithMany(c => c.Notes)
            .HasForeignKey(n => n.CaseId);
    }
}
