using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Infrastructure.Persistence.Configurations;

internal sealed class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("Inspections");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title).HasMaxLength(Inspection.TitleMaxLength).IsRequired();
        builder.Property(i => i.InspectorName).HasMaxLength(Inspection.InspectorNameMaxLength).IsRequired();
        builder.Property(i => i.Notes).HasMaxLength(Inspection.NotesMaxLength);

        // Stored as readable strings rather than magic numbers.
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasIndex(i => new { i.SiteId, i.Status });
        builder.HasIndex(i => i.ScheduledForUtc);

        builder.HasMany(i => i.Findings)
            .WithOne(f => f.Inspection)
            .HasForeignKey(f => f.InspectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(i => i.Findings).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
