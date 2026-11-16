using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace SiteInspectionTracker.Infrastructure.Persistence;

/// <summary>
/// SQL Server's <c>datetime2</c> does not store a <see cref="DateTimeKind"/>, so values come back as
/// <see cref="DateTimeKind.Unspecified"/> and would serialize without a "Z" suffix. All timestamps in
/// this system are UTC, so they are normalised on write and stamped as UTC on read.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            v => v.Kind == DateTimeKind.Local ? v.ToUniversalTime() : v,
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
