using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PROCTOR.Domain.Entities;

namespace PROCTOR.Infrastructure.Data.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(d => d.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(d => d.Type)
            .HasConversion<string>();

        builder.HasOne(d => d.Case)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.CaseId);
    }
}
