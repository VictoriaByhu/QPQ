using FluentValidation;
using Matching.Application.Common.Validation;
using Matching.Domain.Enums;
using Matching.Domain.Interfaces;

namespace Matching.Application.IndexEntries.Commands.CreateIndexEntry;

public sealed class CreateIndexEntryCommandValidator : AbstractValidator<CreateIndexEntryCommand>
{
    public CreateIndexEntryCommandValidator(IUserSkillIndexRepository index)
    {
        // Асинхронна перевірка з запитом до MongoDB виконується лише для коректного UserSkillId.
        RuleFor(x => x.UserSkillId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0)
            .MustAsync(async (id, ct) => !await index.ExistsByUserSkillIdAsync(id, ct))
            .WithMessage("An index entry for this UserSkillId already exists");

        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.SkillId).GreaterThan(0);
        RuleFor(x => x.SkillName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Type).MustBeEnumName<CreateIndexEntryCommand, UserSkillType>();
        RuleFor(x => x.Level).InclusiveBetween(1, 5);
        RuleFor(x => x.Status).MustBeEnumName<CreateIndexEntryCommand, UserSkillStatus>();
        RuleFor(x => x.ExperienceYears).InclusiveBetween(0, 80).When(x => x.ExperienceYears is not null);
        RuleFor(x => x.ExperienceYears).Null()
            .When(x => x.Type == nameof(UserSkillType.Want))
            .WithMessage("ExperienceYears is allowed only for Offer");

        RuleFor(x => x.Languages).NotNull().Must(l => l is null || l.Count <= 10).WithMessage("At most 10 languages");
        RuleForEach(x => x.Languages).SetValidator(new LanguageItemValidator());
        RuleFor(x => x.Languages)
            .Must(l => l.Select(i => i.Code?.Trim().ToLowerInvariant()).Distinct().Count() == l.Count)
            .When(x => x.Languages is not null).WithMessage("Language codes must be unique");

        RuleFor(x => x.Availability).NotNull().Must(a => a is null || a.Count <= 21).WithMessage("At most 21 availability slots");
        RuleForEach(x => x.Availability).SetValidator(new AvailabilityItemValidator());
    }
}
