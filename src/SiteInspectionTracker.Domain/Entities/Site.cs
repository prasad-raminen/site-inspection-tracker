using SiteInspectionTracker.Domain.Common;
using SiteInspectionTracker.Domain.Exceptions;

namespace SiteInspectionTracker.Domain.Entities;

/// <summary>
/// A physical construction or industrial location that is inspected over time.
/// </summary>
public class Site : Entity
{
    public const int NameMaxLength = 200;
    public const int AddressMaxLength = 500;
    public const int ClientMaxLength = 200;

    private readonly List<Inspection> _inspections = new();

    // Required by EF Core.
    private Site()
    {
        Name = string.Empty;
        Address = string.Empty;
    }

    public Site(string name, string address, string? clientName = null, double? latitude = null, double? longitude = null)
    {
        Name = Guard.NotBlank(name, nameof(Name), NameMaxLength);
        Address = Guard.NotBlank(address, nameof(Address), AddressMaxLength);
        ClientName = Guard.Optional(clientName, nameof(ClientName), ClientMaxLength);
        SetCoordinates(latitude, longitude);
    }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public string? ClientName { get; private set; }

    public double? Latitude { get; private set; }

    public double? Longitude { get; private set; }

    public IReadOnlyCollection<Inspection> Inspections => _inspections.AsReadOnly();

    public void Update(string name, string address, string? clientName, double? latitude, double? longitude)
    {
        Name = Guard.NotBlank(name, nameof(Name), NameMaxLength);
        Address = Guard.NotBlank(address, nameof(Address), AddressMaxLength);
        ClientName = Guard.Optional(clientName, nameof(ClientName), ClientMaxLength);
        SetCoordinates(latitude, longitude);
        Touch();
    }

    private void SetCoordinates(double? latitude, double? longitude)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new DomainException("Latitude and longitude must be provided together.");
        }

        if (latitude is < -90 or > 90)
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180)
        {
            throw new DomainException("Longitude must be between -180 and 180.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }
}
