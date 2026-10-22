using FluentValidation;
using Sessions.Bll.Dtos;
using Sessions.Domain.Constants;

namespace Sessions.Bll.Validation;

public class SessionPlaceValidator : AbstractValidator<ISessionPlaceInput>
{
    public SessionPlaceValidator()
    {
        RuleFor(x => x.FormatId)
            .Must(id => id is SessionFormatIds.Online or SessionFormatIds.InPerson)
            .WithMessage($"FormatId must be {SessionFormatIds.Online} (Online) or {SessionFormatIds.InPerson} (InPerson).");

        RuleFor(x => x.ScheduledAt)
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("Session must be scheduled in the future.");

        When(x => x.FormatId == SessionFormatIds.Online, () =>
        {
            RuleFor(x => x.Link)
                .NotEmpty().WithMessage("An online session requires a link.")
                .Must(BeHttpUrl).WithMessage("Link must be an absolute http or https URL.");
            RuleFor(x => x.Location)
                .Empty().WithMessage("An online session must not have a location.");
        });

        When(x => x.FormatId == SessionFormatIds.InPerson, () =>
        {
            RuleFor(x => x.Location)
                .NotEmpty().WithMessage("An in-person session requires a location.");
            RuleFor(x => x.Link)
                .Empty().WithMessage("An in-person session must not have a link.");
        });
    }

    private static bool BeHttpUrl(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
