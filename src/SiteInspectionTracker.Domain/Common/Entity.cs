namespace SiteInspectionTracker.Domain.Common;

/// <summary>
/// Base type for all persisted aggregates/entities. Identity is a database-generated integer key.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    protected void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
