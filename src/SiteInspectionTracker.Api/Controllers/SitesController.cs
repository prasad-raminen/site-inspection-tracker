using Microsoft.AspNetCore.Mvc;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Sites;

namespace SiteInspectionTracker.Api.Controllers;

/// <summary>Construction and industrial sites that are inspected.</summary>
[ApiController]
[Route("api/sites")]
[Produces("application/json")]
public sealed class SitesController : ControllerBase
{
    private readonly ISiteService _sites;

    public SitesController(ISiteService sites) => _sites = sites;

    /// <summary>Lists sites, optionally filtered by a search term, ordered by name.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<SiteDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<SiteDto>>> List([FromQuery] SiteQuery query, CancellationToken cancellationToken)
        => Ok(await _sites.ListAsync(query, cancellationToken));

    /// <summary>Gets a single site with inspection and open-finding counts.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<SiteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SiteDto>> Get(int id, CancellationToken cancellationToken)
        => Ok(await _sites.GetAsync(id, cancellationToken));

    /// <summary>Creates a site. Site names must be unique.</summary>
    [HttpPost]
    [ProducesResponseType<SiteDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteDto>> Create(SaveSiteRequest request, CancellationToken cancellationToken)
    {
        var site = await _sites.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = site.Id }, site);
    }

    /// <summary>Replaces a site's details.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType<SiteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SiteDto>> Update(int id, SaveSiteRequest request, CancellationToken cancellationToken)
        => Ok(await _sites.UpdateAsync(id, request, cancellationToken));

    /// <summary>Deletes a site together with its inspections and findings.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await _sites.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
