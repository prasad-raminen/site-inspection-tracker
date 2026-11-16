using Microsoft.AspNetCore.Mvc;
using SiteInspectionTracker.Application.Dashboard;

namespace SiteInspectionTracker.Api.Controllers;

/// <summary>Aggregated figures for the dashboard.</summary>
[ApiController]
[Route("api/dashboard")]
[Produces("application/json")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Counts of inspections per status, open findings per severity and the next scheduled inspections.</summary>
    [HttpGet("summary")]
    [ProducesResponseType<DashboardSummaryDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(CancellationToken cancellationToken)
        => Ok(await _dashboard.GetSummaryAsync(cancellationToken));
}
