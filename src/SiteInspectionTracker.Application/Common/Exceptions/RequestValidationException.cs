namespace SiteInspectionTracker.Application.Common.Exceptions;

/// <summary>
/// Thrown when an incoming request fails validation. Carries field-level errors so the API
/// can return a <c>ValidationProblemDetails</c> (HTTP 400).
/// </summary>
public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public RequestValidationException(string field, string error)
        : this(new Dictionary<string, string[]> { [field] = new[] { error } })
    {
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
