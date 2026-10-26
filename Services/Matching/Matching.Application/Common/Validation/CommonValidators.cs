using System.Text.RegularExpressions;
using FluentValidation;
using MongoDB.Bson;
using Matching.Application.Common.Models;

namespace Matching.Application.Common.Validation;

public sealed class LanguageItemValidator : AbstractValidator<LanguageItem>
{
    public LanguageItemValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(2, 10);
        RuleFor(x => x.Proficiency).InclusiveBetween(1, 5);
    }
}

public sealed partial class AvailabilityItemValidator : AbstractValidator<AvailabilityItem>
{
    public AvailabilityItemValidator()
    {
        RuleFor(x => x.DayOfWeek).InclusiveBetween(1, 7);
        RuleFor(x => x.Start).Matches(TimeFormat()).WithMessage("Start must be in HH:mm format");
        RuleFor(x => x.End).Matches(TimeFormat()).WithMessage("End must be in HH:mm format");
        RuleFor(x => x).Must(x => string.CompareOrdinal(x.Start, x.End) < 0)
            .When(x => TimeFormat().IsMatch(x.Start ?? "") && TimeFormat().IsMatch(x.End ?? ""))
            .WithName("Availability").WithMessage("Start must be earlier than End");
    }

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeFormat();
}

/// <summary>Спільні правила для полів, що повторюються в командах.</summary>
public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, string> MustBeObjectId<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().Must(id => ObjectId.TryParse(id, out _)).WithMessage("'{PropertyName}' must be a valid ObjectId");

    public static IRuleBuilderOptions<T, string> MustBeUserId<T>(this IRuleBuilder<T, string> rule)
        => rule.NotEmpty().Must(v => Guid.TryParseExact(v, "D", out _))
            .WithMessage("'{PropertyName}' must be a GUID like 00000000-0000-0000-0000-000000000000");

    public static IRuleBuilderOptions<T, string> MustBeEnumName<T, TEnum>(this IRuleBuilder<T, string> rule)
        where TEnum : struct, Enum
        => rule.NotEmpty().Must(v => Enum.GetNames<TEnum>().Contains(v))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", Enum.GetNames<TEnum>())}");

    public static IRuleBuilderOptions<T, int> MustBePageSize<T>(this IRuleBuilder<T, int> rule)
        => rule.InclusiveBetween(1, 100);
}
