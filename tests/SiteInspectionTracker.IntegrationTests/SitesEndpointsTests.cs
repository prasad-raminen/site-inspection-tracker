using System.Net;
using System.Text.Json;
using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Sites;
using SiteInspectionTracker.IntegrationTests.Infrastructure;

namespace SiteInspectionTracker.IntegrationTests;

public sealed class SitesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public SitesEndpointsTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Post_creates_site_and_returns_201_with_location()
    {
        var response = await _client.PostJsonAsync("/api/sites", new SaveSiteRequest("Quarry North", "Quarry Rd", "Stoneworks Ltd", 40.71, -74.0));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var site = await response.ReadJsonAsync<SiteDto>();
        Assert.Equal($"/api/sites/{site.Id}", response.Headers.Location?.AbsolutePath);

        var fetched = await (await _client.GetAsync(response.Headers.Location)).ReadJsonAsync<SiteDto>();
        Assert.Equal("Quarry North", fetched.Name);
        Assert.Equal("Stoneworks Ltd", fetched.ClientName);
    }

    [Fact]
    public async Task Post_with_invalid_body_returns_validation_problem_details()
    {
        var response = await _client.PostJsonAsync("/api/sites", new SaveSiteRequest("", "Somewhere", null, 12, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = problem.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Coordinates", out _));
        Assert.Equal(400, problem.RootElement.GetProperty("status").GetInt32());
        Assert.True(problem.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Post_with_duplicate_name_returns_409()
    {
        await _client.CreateSiteAsync("Duplicate Yard");

        var response = await _client.PostJsonAsync("/api/sites", new SaveSiteRequest("Duplicate Yard", "Other", null, null, null));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_unknown_site_returns_404_problem()
    {
        var response = await _client.GetAsync("/api/sites/987654");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains("987654", problem.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task List_supports_search_and_paging()
    {
        await _client.CreateSiteAsync("Paging Depot 1");
        await _client.CreateSiteAsync("Paging Depot 2");
        await _client.CreateSiteAsync("Paging Depot 3");

        var response = await _client.GetAsync("/api/sites?search=paging%20depot&page=2&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await response.ReadJsonAsync<PagedResult<SiteDto>>();
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal("Paging Depot 3", Assert.Single(page.Items).Name);
    }

    [Fact]
    public async Task Put_updates_and_delete_removes_site()
    {
        var site = await _client.CreateSiteAsync("Temporary Compound");

        var put = await _client.PutJsonAsync($"/api/sites/{site.Id}", new SaveSiteRequest("Renamed Compound", "2 New Rd", "Client", null, null));
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);
        Assert.Equal("Renamed Compound", (await put.ReadJsonAsync<SiteDto>()).Name);

        var delete = await _client.DeleteAsync($"/api/sites/{site.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/sites/{site.Id}")).StatusCode);
    }
}
