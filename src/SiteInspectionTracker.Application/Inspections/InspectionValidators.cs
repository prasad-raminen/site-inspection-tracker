using FluentValidation;
using SiteInspectionTracker.Domain.Entities;

namespace SiteInspectionTracker.Application.Inspections;

public sealed class CreateInspectionRequestValidator : AbstractValidator<CreateInspectionRequest>
{
    public CreateInspectionRequestValidator()
    {
        RuleFor(x => x.SiteId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Inspection.TitleMaxLength);
        RuleFor(x => x.InspectorName).NotEmpty().MaximumLength(Inspection.InspectorNameMaxLength);
        RuleFor(x => x.ScheduledForUtc).NotEqual(default(DateTime)).WithMessage("A scheduled date is required.");
        RuleFor(x => x.Notes).MaximumLength(Inspection.NotesMaxLength);
    }
}

public sealed class UpdateInspectionRequestValidator : AbstractValidator<UpdateInspectionRequest>
{
    public UpdateInspectionRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(Inspection.TitleMaxLength);
        RuleFor(x => x.InspectorName).NotEmpty().MaximumLength(Inspection.InspectorNameMaxLength);
        RuleFor(x => x.ScheduledForUtc).NotEqual(default(DateTime)).WithMessage("A scheduled date is required.");
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(Inspection.NotesMaxLength);
    }
}

public sealed class InspectionQueryValidator : AbstractValidator<InspectionQuery>
{
    public InspectionQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .OverridePropertyName(nameof(InspectionQuery.To))
            .WithMessage("'To' must be on or after 'From'.");
    }
}
