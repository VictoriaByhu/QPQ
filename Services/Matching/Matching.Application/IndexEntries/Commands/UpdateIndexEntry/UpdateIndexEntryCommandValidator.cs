using FluentValidation;
using Matching.Application.Common.Validation;
using Matching.Domain.Enums;

namespace Matching.Application.IndexEntries.Commands.UpdateIndexEntry;

public sealed class UpdateIndexEntryCommandValidator : AbstractValidator<UpdateIndexEntryCommand>
{
    public UpdateIndexEntryCommandValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.ExpectedVersion).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).InclusiveBetween(1, 5);
        RuleFor(x => x.Status).MustBeEnumName<UpdateIndexEntryCommand, UserSkillStatus>();
        RuleFor(x => x.ExperienceYears).InclusiveBetween(0, 80).When(x => x.ExperienceYears is not null);

        RuleFor(x => x.Languages).NotNull().Must(l => l is null || l.Count <= 10).WithMessage("At most 10 languages");
        RuleForEach(x => x.Languages).SetValidator(new LanguageItemValidator());
        RuleFor(x => x.Languages)
            .Must(l => l.Select(i => i.Code?.Trim().ToLowerInvariant()).Distinct().Count() == l.Count)
            .When(x => x.Languages is not null).WithMessage("Language codes must be unique");

        RuleFor(x => x.Availability).NotNull().Must(a => a is null || a.Count <= 21).WithMessage("At most 21 availability slots");
        RuleForEach(x => x.Availability).SetValidator(new AvailabilityItemValidator());
    }
}
