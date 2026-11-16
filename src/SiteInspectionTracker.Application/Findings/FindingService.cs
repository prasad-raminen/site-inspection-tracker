using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Common.Interfaces;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Common.Validation;
using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.Application.Findings;

public sealed class FindingService : IFindingService
{
    internal static readonly Expression<Func<Finding, FindingDto>> ToDto = f => new FindingDto(
        f.Id,
        f.InspectionId,
        f.Inspection!.Title,
        f.Inspection.SiteId,
        f.Inspection.Site!.Name,
        f.Description,
        f.Severity,
        f.IsResolved,
        f.CreatedAtUtc,
        f.ResolvedAtUtc);

    private readonly IApplicationDbContext _db;
    private readonly IValidator<CreateFindingRequest> _createValidator;
    private readonly IValidator<UpdateFindingRequest> _updateValidator;
    private readonly ILogger<FindingService> _logger;

    public FindingService(
        IApplicationDbContext db,
        IValidator<CreateFindingRequest> createValidator,
        IValidator<UpdateFindingRequest> updateValidator,
        ILogger<FindingService> logger)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _logger = logger;
    }

    public Task<PagedResult<FindingDto>> ListAsync(FindingQuery query, CancellationToken cancellationToken = default)
    {
        var findings = _db.Findings.AsNoTracking();

        if (query.SiteId is { } siteId)
        {
            findings = findings.Where(f => f.Inspection!.SiteId == siteId);
        }

        if (query.InspectionId is { } inspectionId)
        {
            findings = findings.Where(f => f.InspectionId == inspectionId);
        }

        if (query.Severity is { } severity)
        {
            findings = findings.Where(f => f.Severity == severity);
        }

        if (query.MinSeverity is { } minSeverity)
        {
            findings = findings.Where(f => f.Severity >= minSeverity);
        }

        if (query.IsResolved is { } isResolved)
        {
            findings = findings.Where(f => f.IsResolved == isResolved);
        }

        return findings
            .OrderBy(f => f.IsResolved)
            .ThenByDescending(f => f.Severity)
            .ThenByDescending(f => f.Id)
            .Select(ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<FindingDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _db.Findings.AsNoTracking()
                   .Where(f => f.Id == id)
                   .Select(ToDto)
                   .SingleOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException(nameof(Finding), id);
    }

    public async Task<FindingDto> CreateAsync(int inspectionId, CreateFindingRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var inspection = await _db.Inspections.SingleOrDefaultAsync(i => i.Id == inspectionId, cancellationToken)
                         ?? throw new NotFoundException(nameof(Inspection), inspectionId);

        Finding finding;
        try
        {
            finding = inspection.AddFinding(request.Description, request.Severity);
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Recorded {Severity} finding {FindingId} on inspection {InspectionId}",
            finding.Severity,
            finding.Id,
            inspectionId);

        return await GetAsync(finding.Id, cancellationToken);
    }

    public async Task<FindingDto> UpdateAsync(int id, UpdateFindingRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var finding = await _db.Findings.SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Finding), id);

        finding.Update(request.Description, request.Severity);
        if (request.IsResolved)
        {
            finding.MarkResolved();
        }
        else
        {
            finding.Reopen();
        }

        await _db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var finding = await _db.Findings.SingleOrDefaultAsync(f => f.Id == id, cancellationToken)
                      ?? throw new NotFoundException(nameof(Finding), id);

        _db.Findings.Remove(finding);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted finding {FindingId}", id);
    }
}
