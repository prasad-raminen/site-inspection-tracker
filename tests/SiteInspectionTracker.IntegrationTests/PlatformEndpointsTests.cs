using System.Net;
using System.Text.Json;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.IntegrationTests.Infrastructure;

namespace SiteInspectionTracker.IntegrationTests;

public sealed class PlatformEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PlatformEndpointsTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_endpoint_reports_healthy_database()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Healthy", body.RootElement.GetProperty("status").GetString());
        Assert.Equal("database", body.RootElement.GetProperty("checks")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Swagger_document_is_generated()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("paths").TryGetProperty("/api/dashboard/summary", out _));
    }

    [Fact]
    public async Task Dashboard_summary_returns_every_status_and_severity_as_named_keys()
    {
        var site = await _client.CreateSiteAsync("Dashboard Site");
        await _client.CreateInspectionAsync(site.Id, DateTime.UtcNow.AddDays(2));

        var response = await _client.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;
        var byStatus = root.GetProperty("inspectionsByStatus");
        foreach (var status in Enum.GetNames<InspectionStatus>())
        {
            Assert.True(byStatus.TryGetProperty(status, out _), $"missing status {status}");
        }

        foreach (var severity in Enum.GetNames<FindingSeverity>())
        {
            Assert.True(root.GetProperty("openFindingsBySeverity").TryGetProperty(severity, out _), $"missing severity {severity}");
        }

        Assert.True(byStatus.GetProperty("Scheduled").GetInt32() >= 1);
        Assert.Equal("Scheduled", root.GetProperty("upcomingInspections")[0].GetProperty("status").GetString());
    }

    [Fact]
    public async Task Cors_preflight_from_the_client_origin_is_allowed()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/sites");
        request.Headers.Add("Origin", ApiFactory.AllowedOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(ApiFactory.AllowedOrigin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
    }

    [Fact]
    public async Task Cors_rejects_unknown_origins()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/sites");
        request.Headers.Add("Origin", "https://evil.example.com");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
