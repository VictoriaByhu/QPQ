using FluentValidation;
using Sessions.Bll.Dtos;

namespace Sessions.Bll.Validation;

public class CreateSessionDtoValidator : AbstractValidator<CreateSessionDto>
{
    public CreateSessionDtoValidator()
    {
        Include(new SessionPlaceValidator());

        RuleFor(x => x.Skills)
            .Must(skills => skills.Select(s => s.SkillId).Distinct().Count() == skills.Count)
            .WithMessage("Each skill can be planned only once per session.");

        RuleFor(x => x.Skills)
            .Must((dto, skills) => skills.Sum(s => s.PlannedMinutes) <= dto.DurationMinutes)
            .WithMessage("Planned skill minutes must not exceed the session duration.");
    }
}
