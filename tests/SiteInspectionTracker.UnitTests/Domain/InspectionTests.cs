using SiteInspectionTracker.Domain.Entities;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.UnitTests.Domain;

public class InspectionTests
{
    private static Inspection NewInspection() =>
        new(siteId: 1, "Crane foundation check", "Alex Smith", new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void New_inspection_starts_scheduled_with_trimmed_values()
    {
        var inspection = new Inspection(1, "  Crane check  ", "  Alex  ", DateTime.UtcNow, "   ");

        Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
        Assert.Equal("Crane check", inspection.Title);
        Assert.Equal("Alex", inspection.InspectorName);
        Assert.Null(inspection.Notes);
        Assert.Null(inspection.CompletedAtUtc);
    }

    [Fact]
    public void Unspecified_date_kind_is_treated_as_utc()
    {
        var unspecified = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Unspecified);

        var inspection = new Inspection(1, "Title", "Inspector", unspecified);

        Assert.Equal(DateTimeKind.Utc, inspection.ScheduledForUtc.Kind);
        Assert.Equal(unspecified.Ticks, inspection.ScheduledForUtc.Ticks);
    }

    [Theory]
    [InlineData(InspectionStatus.Scheduled, InspectionStatus.InProgress)]
    [InlineData(InspectionStatus.InProgress, InspectionStatus.Completed)]
    [InlineData(InspectionStatus.InProgress, InspectionStatus.Failed)]
    [InlineData(InspectionStatus.Failed, InspectionStatus.Scheduled)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.Completed)]
    public void CanTransition_allows_valid_transitions(InspectionStatus from, InspectionStatus to)
    {
        Assert.True(Inspection.CanTransition(from, to));
    }

    [Theory]
    [InlineData(InspectionStatus.Scheduled, InspectionStatus.Completed)]
    [InlineData(InspectionStatus.Scheduled, InspectionStatus.Failed)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.InProgress)]
    [InlineData(InspectionStatus.Completed, InspectionStatus.Scheduled)]
    [InlineData(InspectionStatus.InProgress, InspectionStatus.Scheduled)]
    public void CanTransition_rejects_invalid_transitions(InspectionStatus from, InspectionStatus to)
    {
        Assert.False(Inspection.CanTransition(from, to));
    }

    [Fact]
    public void ChangeStatus_to_an_illegal_state_throws_and_keeps_current_status()
    {
        var inspection = NewInspection();

        var ex = Assert.Throws<DomainException>(() => inspection.ChangeStatus(InspectionStatus.Completed));

        Assert.Contains("Scheduled", ex.Message);
        Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
    }

    [Fact]
    public void Completing_sets_completion_time_and_rescheduling_after_failure_clears_it()
    {
        var inspection = NewInspection();

        inspection.ChangeStatus(InspectionStatus.InProgress);
        inspection.ChangeStatus(InspectionStatus.Failed);
        Assert.NotNull(inspection.CompletedAtUtc);

        inspection.ChangeStatus(InspectionStatus.Scheduled);
        Assert.Null(inspection.CompletedAtUtc);
        Assert.Equal(InspectionStatus.Scheduled, inspection.Status);
    }

    [Fact]
    public void AddFinding_before_inspection_has_started_throws()
    {
        var inspection = NewInspection();

        Assert.Throws<DomainException>(() => inspection.AddFinding("Loose handrail", FindingSeverity.Medium));
        Assert.Empty(inspection.Findings);
    }

    [Fact]
    public void AddFinding_on_started_inspection_records_an_open_finding()
    {
        var inspection = NewInspection();
        inspection.ChangeStatus(InspectionStatus.InProgress);

        var finding = inspection.AddFinding("  Loose handrail  ", FindingSeverity.High);

        Assert.Single(inspection.Findings);
        Assert.Equal("Loose handrail", finding.Description);
        Assert.Equal(FindingSeverity.High, finding.Severity);
        Assert.False(finding.IsResolved);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_title_is_rejected(string title)
    {
        Assert.Throws<DomainException>(() => new Inspection(1, title, "Inspector", DateTime.UtcNow));
    }
}
