using SiteInspectionTracker.Domain.Common;
using SiteInspectionTracker.Domain.Enums;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.Domain.Entities;

/// <summary>
/// A scheduled inspection of a <see cref="Site"/>. Status changes follow a small state machine:
/// <c>Scheduled → InProgress → Completed | Failed</c>, and a failed inspection may be re-scheduled.
/// </summary>
public class Inspection : Entity
{
    public const int TitleMaxLength = 200;
    public const int InspectorNameMaxLength = 120;
    public const int NotesMaxLength = 4000;

    private static readonly IReadOnlyDictionary<InspectionStatus, InspectionStatus[]> AllowedTransitions =
        new Dictionary<InspectionStatus, InspectionStatus[]>
        {
            [InspectionStatus.Scheduled] = new[] { InspectionStatus.InProgress },
            [InspectionStatus.InProgress] = new[] { InspectionStatus.Completed, InspectionStatus.Failed },
            [InspectionStatus.Failed] = new[] { InspectionStatus.Scheduled },
            [InspectionStatus.Completed] = Array.Empty<InspectionStatus>(),
        };

    private readonly List<Finding> _findings = new();

    // Required by EF Core.
    private Inspection()
    {
        Title = string.Empty;
        InspectorName = string.Empty;
    }

    public Inspection(int siteId, string title, string inspectorName, DateTime scheduledForUtc, string? notes = null)
    {
        SiteId = siteId;
        Title = Guard.NotBlank(title, nameof(Title), TitleMaxLength);
        InspectorName = Guard.NotBlank(inspectorName, nameof(InspectorName), InspectorNameMaxLength);
        ScheduledForUtc = EnsureUtc(scheduledForUtc);
        Notes = Guard.Optional(notes, nameof(Notes), NotesMaxLength);
        Status = InspectionStatus.Scheduled;
    }

    public int SiteId { get; private set; }

    public Site? Site { get; private set; }

    public string Title { get; private set; }

    public string InspectorName { get; private set; }

    public DateTime ScheduledForUtc { get; private set; }

    public DateTime? CompletedAtUtc { get; private set; }

    public InspectionStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<Finding> Findings => _findings.AsReadOnly();

    public static bool CanTransition(InspectionStatus from, InspectionStatus to) =>
        from == to || AllowedTransitions[from].Contains(to);

    public void UpdateDetails(string title, string inspectorName, DateTime scheduledForUtc, string? notes)
    {
        Title = Guard.NotBlank(title, nameof(Title), TitleMaxLength);
        InspectorName = Guard.NotBlank(inspectorName, nameof(InspectorName), InspectorNameMaxLength);
        ScheduledForUtc = EnsureUtc(scheduledForUtc);
        Notes = Guard.Optional(notes, nameof(Notes), NotesMaxLength);
        Touch();
    }

    public void ChangeStatus(InspectionStatus newStatus)
    {
        if (newStatus == Status)
        {
            return;
        }

        if (!CanTransition(Status, newStatus))
        {
            throw new DomainException($"Cannot change inspection status from {Status} to {newStatus}.");
        }

        Status = newStatus;
        CompletedAtUtc = newStatus is InspectionStatus.Completed or InspectionStatus.Failed
            ? DateTime.UtcNow
            : null;
        Touch();
    }

    public Finding AddFinding(string description, FindingSeverity severity)
    {
        if (Status == InspectionStatus.Scheduled)
        {
            throw new DomainException("Findings can only be recorded once an inspection has started.");
        }

        var finding = new Finding(Id, description, severity);
        _findings.Add(finding);
        Touch();
        return finding;
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };
}
