using SiteInspectionTracker.Application.Common.Models;

namespace SiteInspectionTracker.Application.Inspections;

public interface IInspectionService
{
    Task<PagedResult<InspectionDto>> ListAsync(InspectionQuery query, CancellationToken cancellationToken = default);

    Task<InspectionDetailDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<InspectionDto> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken = default);

    Task<InspectionDto> UpdateAsync(int id, UpdateInspectionRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
