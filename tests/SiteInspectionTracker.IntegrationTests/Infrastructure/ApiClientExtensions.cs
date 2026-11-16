using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Application.Sites;

namespace SiteInspectionTracker.IntegrationTests.Infrastructure;

internal static class ApiClientExtensions
{
    public static readonly JsonSerializerOptions Json = CreateOptions();

    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        return value ?? throw new InvalidOperationException($"Response body was not a {typeof(T).Name}.");
    }

    public static Task<HttpResponseMessage> PostJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PostAsJsonAsync(url, body, Json);

    public static Task<HttpResponseMessage> PutJsonAsync<T>(this HttpClient client, string url, T body) =>
        client.PutAsJsonAsync(url, body, Json);

    public static async Task<SiteDto> CreateSiteAsync(this HttpClient client, string name)
    {
        var response = await client.PostJsonAsync("/api/sites", new SaveSiteRequest(name, "1 Test Street", null, null, null));
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<SiteDto>();
    }

    public static async Task<InspectionDto> CreateInspectionAsync(this HttpClient client, int siteId, DateTime? scheduledForUtc = null)
    {
        var request = new CreateInspectionRequest(siteId, "Safety walk", "Jordan Blake", scheduledForUtc ?? DateTime.UtcNow.AddDays(1), null);
        var response = await client.PostJsonAsync("/api/inspections", request);
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<InspectionDto>();
    }

    public static async Task<FindingDto> CreateFindingAsync(this HttpClient client, int inspectionId, string description, Domain.Enums.FindingSeverity severity)
    {
        var response = await client.PostJsonAsync($"/api/inspections/{inspectionId}/findings", new CreateFindingRequest(description, severity));
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<FindingDto>();
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
