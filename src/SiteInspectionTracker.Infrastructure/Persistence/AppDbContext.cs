using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Application.Common.Interfaces;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Infrastructure.Persistence;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Site> Sites => Set<Site>();

    public DbSet<Inspection> Inspections => Set<Inspection>();

    public DbSet<Finding> Findings => Set<Finding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
