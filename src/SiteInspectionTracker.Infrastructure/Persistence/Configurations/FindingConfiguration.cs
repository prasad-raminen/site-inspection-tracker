using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Infrastructure.Persistence.Configurations;

internal sealed class FindingConfiguration : IEntityTypeConfiguration<Finding>
{
    public void Configure(EntityTypeBuilder<Finding> builder)
    {
        builder.ToTable("Findings");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Description).HasMaxLength(Finding.DescriptionMaxLength).IsRequired();
        // Severity is ordinal (Low < Medium < High < Critical), so it is stored as an int to keep
        // ORDER BY and range filters meaningful in SQL.
        builder.Property(f => f.Severity).IsRequired();

        builder.HasIndex(f => new { f.InspectionId, f.Severity });
        builder.HasIndex(f => f.IsResolved);
    }
}
