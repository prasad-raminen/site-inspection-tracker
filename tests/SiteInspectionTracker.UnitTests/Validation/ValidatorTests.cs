using SiteInspectionTracker.Application.Common.Models;
using SiteInspectionTracker.Application.Findings;
using SiteInspectionTracker.Application.Inspections;
using SiteInspectionTracker.Application.Sites;
using SiteInspectionTracker.Domain.Enums;

namespace SiteInspectionTracker.UnitTests.Validation;

public class ValidatorTests
{
    [Fact]
    public void Site_request_with_missing_name_and_half_coordinates_is_invalid()
    {
        var result = new SaveSiteRequestValidator().Validate(new SaveSiteRequest("", "1 Road", null, 45, null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SaveSiteRequest.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == "Coordinates");
    }

    [Fact]
    public void Valid_site_request_passes()
    {
        var result = new SaveSiteRequestValidator().Validate(new SaveSiteRequest("Depot", "1 Road", "Acme", 45, 120));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Create_inspection_requires_site_title_inspector_and_date()
    {
        var result = new CreateInspectionRequestValidator().Validate(new CreateInspectionRequest(0, "", " ", default, null));

        var failed = result.Errors.Select(e => e.PropertyName).ToHashSet();
        Assert.Equal(
            new[] { "InspectorName", "ScheduledForUtc", "SiteId", "Title" },
            failed.OrderBy(p => p, StringComparer.Ordinal));
    }

    [Fact]
    public void Update_inspection_rejects_undefined_status_value()
    {
        var request = new UpdateInspectionRequest("Title", "Inspector", DateTime.UtcNow, (InspectionStatus)42, null);

        var result = new UpdateInspectionRequestValidator().Validate(request);

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateInspectionRequest.Status));
    }

    [Fact]
    public void Inspection_query_rejects_inverted_date_range()
    {
        var query = new InspectionQuery { From = new DateTime(2026, 2, 1), To = new DateTime(2026, 1, 1) };

        var result = new InspectionQueryValidator().Validate(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Finding_request_requires_description()
    {
        var result = new CreateFindingRequestValidator().Validate(new CreateFindingRequest("", FindingSeverity.Low));

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateFindingRequest.Description));
    }

    [Theory]
    [InlineData(0, 0, 1, PageRequest.DefaultPageSize)]
    [InlineData(-5, 500, 1, PageRequest.MaxPageSize)]
    [InlineData(3, 10, 3, 10)]
    public void Page_request_clamps_out_of_range_values(int page, int pageSize, int expectedPage, int expectedSize)
    {
        var query = new SiteQuery { Page = page, PageSize = pageSize };

        Assert.Equal(expectedPage, query.Page);
        Assert.Equal(expectedSize, query.PageSize);
        Assert.Equal((expectedPage - 1) * expectedSize, query.Skip);
    }
}
