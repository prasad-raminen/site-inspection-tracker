using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Common.Interfaces;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Common.Validation;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Application.Sites;

public sealed class SiteService : ISiteService
{
    internal static readonly Expression<Func<Site, SiteDto>> ToDto = s => new SiteDto(
        s.Id,
        s.Name,
        s.Address,
        s.ClientName,
        s.Latitude,
        s.Longitude,
        s.Inspections.Count,
        s.Inspections.SelectMany(i => i.Findings).Count(f => !f.IsResolved),
        s.CreatedAtUtc,
        s.UpdatedAtUtc);

    private readonly IApplicationDbContext _db;
    private readonly IValidator<SaveSiteRequest> _validator;
    private readonly ILogger<SiteService> _logger;

    public SiteService(IApplicationDbContext db, IValidator<SaveSiteRequest> validator, ILogger<SiteService> logger)
    {
        _db = db;
        _validator = validator;
        _logger = logger;
    }

    public Task<PagedResult<SiteDto>> ListAsync(SiteQuery query, CancellationToken cancellationToken = default)
    {
        var sites = _db.Sites.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            sites = sites.Where(s =>
                s.Name.ToLower().Contains(term) ||
                s.Address.ToLower().Contains(term) ||
                (s.ClientName != null && s.ClientName.ToLower().Contains(term)));
        }

        return sites
            .OrderBy(s => s.Name)
            .Select(ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<SiteDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Sites.AsNoTracking()
                   .Where(s => s.Id == id)
                   .Select(ToDto)
                   .SingleOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException(nameof(Site), id);
    }

    public async Task<SiteDto> CreateAsync(SaveSiteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.EnsureValidAsync(request, cancellationToken);
        await EnsureNameIsUniqueAsync(request.Name, excludeId: null, cancellationToken);

        var site = new Site(request.Name, request.Address, request.ClientName, request.Latitude, request.Longitude);
        _db.Sites.Add(site);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created site {SiteId} ({SiteName})", site.Id, site.Name);
        return await GetAsync(site.Id, cancellationToken);
    }

    public async Task<SiteDto> UpdateAsync(int id, SaveSiteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.EnsureValidAsync(request, cancellationToken);

        var site = await _db.Sites.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Site), id);

        await EnsureNameIsUniqueAsync(request.Name, excludeId: id, cancellationToken);

        site.Update(request.Name, request.Address, request.ClientName, request.Latitude, request.Longitude);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated site {SiteId}", id);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var site = await _db.Sites.SingleOrDefaultAsync(s => s.Id == id, cancellationToken)
                   ?? throw new NotFoundException(nameof(Site), id);

        _db.Sites.Remove(site);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted site {SiteId} and its inspections", id);
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.Trim();
        var exists = await _db.Sites.AnyAsync(
            s => s.Name == normalized && (excludeId == null || s.Id != excludeId),
            cancellationToken);

        if (exists)
        {
            throw new ConflictException($"A site named '{normalized}' already exists.");
        }
    }
}
