using Microsoft.AspNetCore.Mvc;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Application.Inspections;

namespace SiteInspectionTracker.Api.Controllers;

/// <summary>Inspections scheduled against sites, and the findings recorded during them.</summary>
[ApiController]
[Route("api/inspections")]
[Produces("application/json")]
public sealed class InspectionsController : ControllerBase
{
    private readonly IInspectionService _inspections;
    private readonly IFindingService _findings;

    public InspectionsController(IInspectionService inspections, IFindingService findings)
    {
        _inspections = inspections;
        _findings = findings;
    }

    /// <summary>Lists inspections (newest scheduled first) filtered by site, status, inspector or date range.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<InspectionDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<InspectionDto>>> List([FromQuery] InspectionQuery query, CancellationToken cancellationToken)
        => Ok(await _inspections.ListAsync(query, cancellationToken));

    /// <summary>Gets an inspection together with its findings.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<InspectionDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InspectionDetailDto>> Get(int id, CancellationToken cancellationToken)
        => Ok(await _inspections.GetAsync(id, cancellationToken));

    /// <summary>Schedules a new inspection for a site. New inspections start in the Scheduled status.</summary>
    [HttpPost]
    [ProducesResponseType<InspectionDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InspectionDto>> Create(CreateInspectionRequest request, CancellationToken cancellationToken)
    {
        var inspection = await _inspections.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = inspection.Id }, inspection);
    }

    /// <summary>
    /// Updates an inspection. Status changes must follow Scheduled → InProgress → Completed/Failed
    /// (Failed may be re-scheduled); illegal transitions return 409.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<InspectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InspectionDto>> Update(int id, UpdateInspectionRequest request, CancellationToken cancellationToken)
        => Ok(await _inspections.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes an inspection and its findings.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _inspections.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    /// <summary>Lists the findings recorded for an inspection.</summary>
    [HttpGet("{id:int}/findings")]
    [ProducesResponseType<PagedResult<FindingDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<FindingDto>>> ListFindings(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        // Resolve the inspection first so an unknown id yields 404 rather than an empty page.
        await _inspections.GetAsync(id, cancellationToken);
        var query = new FindingQuery { InspectionId = id, Page = page, PageSize = pageSize };
        return Ok(await _findings.ListAsync(query, cancellationToken));
    }

    /// <summary>Records a finding on an inspection that has started.</summary>
    [HttpPost("{id:int}/findings")]
    [ProducesResponseType<FindingDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FindingDto>> CreateFinding(int id, CreateFindingRequest request, CancellationToken cancellationToken)
    {
        var finding = await _findings.CreateAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(FindingsController.Get), "Findings", new { id = finding.Id }, finding);
    }
}
