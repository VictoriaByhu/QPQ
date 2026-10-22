using FluentValidation;
using Sessions.Bll.Dtos;
using Sessions.Bll.Specifications;
using Sessions.Domain.Constants;

namespace Sessions.Bll.Validation;

public class SessionQueryDtoValidator : AbstractValidator<SessionQueryDto>
{
    public SessionQueryDtoValidator()
    {
        RuleFor(x => x.Status)
            .Must(status => SessionStatuses.All.Contains(status!))
            .When(x => !string.IsNullOrWhiteSpace(x.Status))
            .WithMessage($"Status must be one of: {string.Join(", ", SessionStatuses.All)}.");

        RuleFor(x => x.SortBy)
            .Must(sortBy => SessionsFilterSpec.SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", SessionsFilterSpec.SortFields)}.");

        RuleFor(x => x.SortDir)
            .Must(dir => dir is not null && (dir.Equals("asc", StringComparison.OrdinalIgnoreCase)
                                             || dir.Equals("desc", StringComparison.OrdinalIgnoreCase)))
            .WithMessage("SortDir must be 'asc' or 'desc'.");

        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .When(x => x.From is not null && x.To is not null)
            .WithMessage("'To' must not be earlier than 'From'.");
    }
}
