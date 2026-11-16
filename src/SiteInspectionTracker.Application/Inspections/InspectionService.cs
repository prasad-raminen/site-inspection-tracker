using System.Linq.Expressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SiteInspectionTracker.Application.Common.Exceptions;
using SiteInspectionTracker.Application.Common.Interfaces;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Common.Validation;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.Application.Inspections;

public sealed class InspectionService : IInspectionService
{
    internal static readonly Expression<Func<Inspection, InspectionDto>> ToDto = i => new InspectionDto(
        i.Id,
        i.SiteId,
        i.Site!.Name,
        i.Title,
        i.InspectorName,
        i.ScheduledForUtc,
        i.CompletedAtUtc,
        i.Status,
        i.Notes,
        i.Findings.Count,
        i.Findings.Count(f => !f.IsResolved),
        i.CreatedAtUtc,
        i.UpdatedAtUtc);

    private readonly IApplicationDbContext _db;
    private readonly IValidator<CreateInspectionRequest> _createValidator;
    private readonly IValidator<UpdateInspectionRequest> _updateValidator;
    private readonly IValidator<InspectionQuery> _queryValidator;
    private readonly ILogger<InspectionService> _logger;

    public InspectionService(
        IApplicationDbContext db,
        IValidator<CreateInspectionRequest> createValidator,
        IValidator<UpdateInspectionRequest> updateValidator,
        IValidator<InspectionQuery> queryValidator,
        ILogger<InspectionService> logger)
    {
        _db = db;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _queryValidator = queryValidator;
        _logger = logger;
    }

    public async Task<PagedResult<InspectionDto>> ListAsync(InspectionQuery query, CancellationToken cancellationToken = default)
    {
        await _queryValidator.EnsureValidAsync(query, cancellationToken);

        var inspections = _db.Inspections.AsNoTracking();

        if (query.SiteId is { } siteId)
        {
            inspections = inspections.Where(i => i.SiteId == siteId);
        }

        if (query.Status is { } status)
        {
            inspections = inspections.Where(i => i.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Inspector))
        {
            var term = query.Inspector.Trim().ToLower();
            inspections = inspections.Where(i => i.InspectorName.ToLower().Contains(term));
        }

        if (query.From is { } from)
        {
            inspections = inspections.Where(i => i.ScheduledForUtc >= from);
        }

        if (query.To is { } to)
        {
            inspections = inspections.Where(i => i.ScheduledForUtc <= to);
        }

        return await inspections
            .OrderByDescending(i => i.ScheduledForUtc)
            .ThenBy(i => i.Id)
            .Select(ToDto)
            .ToPagedResultAsync(query, cancellationToken);
    }

    public async Task<InspectionDetailDto> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var inspection = await GetDtoAsync(id, cancellationToken);

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.InspectionId == id)
            .OrderByDescending(f => f.Severity)
            .ThenBy(f => f.Id)
            .Select(FindingService.ToDto)
            .ToListAsync(cancellationToken);

        return new InspectionDetailDto(inspection, findings);
    }

    public async Task<InspectionDto> CreateAsync(CreateInspectionRequest request, CancellationToken cancellationToken = default)
    {
        await _createValidator.EnsureValidAsync(request, cancellationToken);

        var siteExists = await _db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken);
        if (!siteExists)
        {
            throw new RequestValidationException(nameof(request.SiteId), $"Site {request.SiteId} does not exist.");
        }

        var inspection = new Inspection(request.SiteId, request.Title, request.InspectorName, request.ScheduledForUtc, request.Notes);
        _db.Inspections.Add(inspection);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Scheduled inspection {InspectionId} for site {SiteId} on {ScheduledForUtc:u}",
            inspection.Id,
            inspection.SiteId,
            inspection.ScheduledForUtc);

        return await GetDtoAsync(inspection.Id, cancellationToken);
    }

    public async Task<InspectionDto> UpdateAsync(int id, UpdateInspectionRequest request, CancellationToken cancellationToken = default)
    {
        await _updateValidator.EnsureValidAsync(request, cancellationToken);

        var inspection = await _db.Inspections.SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
                         ?? throw new NotFoundException(nameof(Inspection), id);

        var previousStatus = inspection.Status;
        try
        {
            inspection.UpdateDetails(request.Title, request.InspectorName, request.ScheduledForUtc, request.Notes);
            inspection.ChangeStatus(request.Status);
        }
        catch (DomainException ex)
        {
            throw new ConflictException(ex.Message);
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (previousStatus != inspection.Status)
        {
            _logger.LogInformation(
                "Inspection {InspectionId} moved from {PreviousStatus} to {Status}",
                id,
                previousStatus,
                inspection.Status);
        }

        return await GetDtoAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var inspection = await _db.Inspections.SingleOrDefaultAsync(i => i.Id == id, cancellationToken)
                         ?? throw new NotFoundException(nameof(Inspection), id);

        _db.Inspections.Remove(inspection);
        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted inspection {InspectionId}", id);
    }

    private async Task<InspectionDto> GetDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await _db.Inspections.AsNoTracking()
                   .Where(i => i.Id == id)
                   .Select(ToDto)
                   .SingleOrDefaultAsync(cancellationToken)
               ?? throw new NotFoundException(nameof(Inspection), id);
    }
}
