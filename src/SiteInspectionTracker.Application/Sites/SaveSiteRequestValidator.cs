using FluentValidation;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Application.Sites;

public sealed class SaveSiteRequestValidator : AbstractValidator<SaveSiteRequest>
{
    public SaveSiteRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Site.NameMaxLength);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(Site.AddressMaxLength);
        RuleFor(x => x.ClientName).MaximumLength(Site.ClientMaxLength);

        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);

        RuleFor(x => x)
            .Must(x => x.Latitude.HasValue == x.Longitude.HasValue)
            .OverridePropertyName("Coordinates")
            .WithMessage("Latitude and longitude must be provided together.");
    }
}
