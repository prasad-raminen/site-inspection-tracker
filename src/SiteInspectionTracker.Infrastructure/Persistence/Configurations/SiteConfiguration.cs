using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Infrastructure.Persistence.Configurations;

internal sealed class SiteConfiguration : IEntityTypeConfiguration<Site>
{
    public void Configure(EntityTypeBuilder<Site> builder)
    {
        builder.ToTable("Sites");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).HasMaxLength(Site.NameMaxLength).IsRequired();
        builder.Property(s => s.Address).HasMaxLength(Site.AddressMaxLength).IsRequired();
        builder.Property(s => s.ClientName).HasMaxLength(Site.ClientMaxLength);

        builder.HasIndex(s => s.Name).IsUnique();

        builder.HasMany(s => s.Inspections)
            .WithOne(i => i.Site)
            .HasForeignKey(i => i.SiteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Inspections).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
