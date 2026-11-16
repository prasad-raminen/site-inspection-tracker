using SiteInspectionTracker.Application.Common.Models;

namespace SiteInspectionTracker.Application.Findings;

public interface IFindingService
{
    Task<PagedResult<FindingDto>> ListAsync(FindingQuery query, CancellationToken cancellationToken = default);

    Task<FindingDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<FindingDto> CreateAsync(int inspectionId, CreateFindingRequest request, CancellationToken cancellationToken = default);

    Task<FindingDto> UpdateAsync(int id, UpdateFindingRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
