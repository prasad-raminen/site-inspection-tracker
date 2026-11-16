using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.UnitTests.Domain;

public class SiteAndFindingTests
{
    [Fact]
    public void Site_requires_both_coordinates_or_neither()
    {
        Assert.Throws<DomainException>(() => new Site("Depot", "1 Road", latitude: 10, longitude: null));

        var site = new Site("Depot", "1 Road", latitude: 10, longitude: 20);
        Assert.Equal(10, site.Latitude);
        Assert.Equal(20, site.Longitude);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(0, -181)]
    public void Site_rejects_out_of_range_coordinates(double latitude, double longitude)
    {
        Assert.Throws<DomainException>(() => new Site("Depot", "1 Road", latitude: latitude, longitude: longitude));
    }

    [Fact]
    public void Site_name_longer_than_limit_is_rejected()
    {
        var name = new string('x', Site.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new Site(name, "1 Road"));
    }

    [Fact]
    public void Finding_can_be_resolved_and_reopened()
    {
        var inspection = new Inspection(1, "Check", "Inspector", DateTime.UtcNow);
        inspection.ChangeStatus(InspectionStatus.InProgress);
        var finding = inspection.AddFinding("Exposed rebar", FindingSeverity.Medium);

        finding.MarkResolved();
        Assert.True(finding.IsResolved);
        Assert.NotNull(finding.ResolvedAtUtc);

        finding.Reopen();
        Assert.False(finding.IsResolved);
        Assert.Null(finding.ResolvedAtUtc);
    }
}
