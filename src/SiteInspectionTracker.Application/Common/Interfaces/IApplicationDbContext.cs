using Microsoft.EntityFrameworkCore;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Application.Common.Interfaces;

/// <summary>
/// Persistence abstraction used by application services. Implemented by the EF Core
/// context in the Infrastructure layer so the Application layer never depends on a provider.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Site> Sites { get; }

    DbSet<Inspection> Inspections { get; }

    DbSet<Finding> Findings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
