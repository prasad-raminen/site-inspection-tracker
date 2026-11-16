namespace SiteInspectionTracker.Domain.Exceptions;

/// <summary>
/// Raised when an operation would violate a business rule of the domain model
/// (for example an illegal inspection status transition).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message)
        : base(message)
    {
    }
}
