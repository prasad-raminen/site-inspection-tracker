using SiteInspectionTracker.Application.Common.Models;

namespace SiteInspectionTracker.Application.Sites;

public interface ISiteService
{
    Task<PagedResult<SiteDto>> ListAsync(SiteQuery query, CancellationToken cancellationToken = default);

    Task<SiteDto> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<SiteDto> CreateAsync(SaveSiteRequest request, CancellationToken cancellationToken = default);

    Task<SiteDto> UpdateAsync(int id, SaveSiteRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
