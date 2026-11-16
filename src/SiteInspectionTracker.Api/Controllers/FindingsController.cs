using Microsoft.AspNetCore.Mvc;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Findings;

namespace SiteInspectionTracker.Api.Controllers;

/// <summary>Findings across all inspections.</summary>
[ApiController]
[Route("api/findings")]
[Produces("application/json")]
public sealed class FindingsController : ControllerBase
{
    private readonly IFindingService _findings;

    public FindingsController(IFindingService findings) => _findings = findings;

    /// <summary>Lists findings (open and most severe first) filtered by site, inspection, severity or resolution.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<FindingDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<FindingDto>>> List([FromQuery] FindingQuery query, CancellationToken cancellationToken)
        => Ok(await _findings.ListAsync(query, cancellationToken));

    /// <summary>Gets a single finding.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<FindingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FindingDto>> Get(int id, CancellationToken cancellationToken)
        => Ok(await _findings.GetAsync(id, cancellationToken));

    /// <summary>Updates a finding's description/severity and marks it resolved or re-opens it.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<FindingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FindingDto>> Update(int id, UpdateFindingRequest request, CancellationToken cancellationToken)
        => Ok(await _findings.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a finding.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _findings.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
