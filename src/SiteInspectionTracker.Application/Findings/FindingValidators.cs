using FluentValidation;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Application.Findings;

public sealed class CreateFindingRequestValidator : AbstractValidator<CreateFindingRequest>
{
    public CreateFindingRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(Finding.DescriptionMaxLength);
        RuleFor(x => x.Severity).IsInEnum();
    }
}

public sealed class UpdateFindingRequestValidator : AbstractValidator<UpdateFindingRequest>
{
    public UpdateFindingRequestValidator()
    {
        RuleFor(x => x.Description).NotEmpty().MaximumLength(Finding.DescriptionMaxLength);
        RuleFor(x => x.Severity).IsInEnum();
    }
}
