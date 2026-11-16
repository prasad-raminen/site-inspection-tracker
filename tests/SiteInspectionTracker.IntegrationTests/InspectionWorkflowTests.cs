using System.Net;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.IntegrationTests.Infrastructure;

namespace SiteInspectionTracker.IntegrationTests;

public sealed class InspectionWorkflowTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public InspectionWorkflowTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Inspection_lifecycle_from_scheduling_to_completion_with_findings()
    {
        var site = await _client.CreateSiteAsync("Lifecycle Plant");
        var inspection = await _client.CreateInspectionAsync(site.Id);
        Assert.Equal(InspectionStatus.Scheduled, inspection.Status);

        // Findings cannot be recorded before the inspection starts.
        var early = await _client.PostJsonAsync($"/api/inspections/{inspection.Id}/findings", new CreateFindingRequest("Too early", FindingSeverity.Low));
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);

        var started = await _client.PutJsonAsync(
            $"/api/inspections/{inspection.Id}",
            new UpdateInspectionRequest(inspection.Title, inspection.InspectorName, inspection.ScheduledForUtc, InspectionStatus.InProgress, "On site"));
        Assert.Equal(HttpStatusCode.OK, started.StatusCode);

        var findingResponse = await _client.PostJsonAsync($"/api/inspections/{inspection.Id}/findings", new CreateFindingRequest("Guard rail missing on level 3", FindingSeverity.Critical));
        Assert.Equal(HttpStatusCode.Created, findingResponse.StatusCode);
        var finding = await findingResponse.ReadJsonAsync<FindingDto>();
        Assert.Equal($"/api/findings/{finding.Id}", findingResponse.Headers.Location?.AbsolutePath);

        var completed = await _client.PutJsonAsync(
            $"/api/inspections/{inspection.Id}",
            new UpdateInspectionRequest(inspection.Title, inspection.InspectorName, inspection.ScheduledForUtc, InspectionStatus.Completed, "Done"));
        var completedDto = await completed.ReadJsonAsync<InspectionDto>();
        Assert.Equal(InspectionStatus.Completed, completedDto.Status);
        Assert.NotNull(completedDto.CompletedAtUtc);

        var detail = await (await _client.GetAsync($"/api/inspections/{inspection.Id}")).ReadJsonAsync<InspectionDetailDto>();
        Assert.Equal(1, detail.Inspection.OpenFindingCount);
        Assert.Equal("Guard rail missing on level 3", Assert.Single(detail.Findings).Description);
    }

    [Fact]
    public async Task Illegal_status_transition_returns_409()
    {
        var site = await _client.CreateSiteAsync("Transition Yard");
        var inspection = await _client.CreateInspectionAsync(site.Id);

        var response = await _client.PutJsonAsync(
            $"/api/inspections/{inspection.Id}",
            new UpdateInspectionRequest(inspection.Title, inspection.InspectorName, inspection.ScheduledForUtc, InspectionStatus.Completed, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Creating_inspection_for_unknown_site_returns_400()
    {
        var response = await _client.PostJsonAsync("/api/inspections", new CreateInspectionRequest(424242, "Title", "Inspector", DateTime.UtcNow, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_filters_by_site_and_status_query_parameters()
    {
        var site = await _client.CreateSiteAsync("Filter Works");
        var first = await _client.CreateInspectionAsync(site.Id);
        await _client.CreateInspectionAsync(site.Id);
        await _client.PutJsonAsync(
            $"/api/inspections/{first.Id}",
            new UpdateInspectionRequest(first.Title, first.InspectorName, first.ScheduledForUtc, InspectionStatus.InProgress, null));

        var response = await _client.GetAsync($"/api/inspections?siteId={site.Id}&status=InProgress");

        var page = await response.ReadJsonAsync<PagedResult<InspectionDto>>();
        Assert.Equal(first.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task List_with_unknown_status_value_returns_400()
    {
        var response = await _client.GetAsync("/api/inspections?status=Exploded");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Resolving_a_finding_removes_it_from_open_findings()
    {
        var site = await _client.CreateSiteAsync("Resolution Depot");
        var inspection = await _client.CreateInspectionAsync(site.Id);
        await _client.PutJsonAsync(
            $"/api/inspections/{inspection.Id}",
            new UpdateInspectionRequest(inspection.Title, inspection.InspectorName, inspection.ScheduledForUtc, InspectionStatus.InProgress, null));
        var finding = await _client.CreateFindingAsync(inspection.Id, "Blocked fire exit", FindingSeverity.High);

        var update = await _client.PutJsonAsync($"/api/findings/{finding.Id}", new UpdateFindingRequest(finding.Description, finding.Severity, IsResolved: true));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var open = await (await _client.GetAsync($"/api/findings?siteId={site.Id}&isResolved=false")).ReadJsonAsync<PagedResult<FindingDto>>();
        Assert.Empty(open.Items);

        var nested = await (await _client.GetAsync($"/api/inspections/{inspection.Id}/findings")).ReadJsonAsync<PagedResult<FindingDto>>();
        Assert.True(Assert.Single(nested.Items).IsResolved);
    }
}
