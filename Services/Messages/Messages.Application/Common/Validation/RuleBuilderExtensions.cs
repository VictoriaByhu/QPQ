using FluentValidation;
using MongoDB.Bson;

namespace Messages.Application.Common.Validation;

public static class RuleBuilderExtensions
{
    public const int MaxPageSize = 100;

    public static bool IsObjectId(string? value) => ObjectId.TryParse(value, out var id) && id != ObjectId.Empty;

    public static bool IsUserId(string? value) => Guid.TryParse(value, out var id) && id != Guid.Empty;

    public static IRuleBuilderOptions<T, string> MustBeObjectId<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(IsObjectId).WithMessage("'{PropertyName}' must be a valid ObjectId (24 hex characters).");

    public static IRuleBuilderOptions<T, string> MustBeUserId<T>(this IRuleBuilder<T, string> rule)
        => rule.Must(IsUserId).WithMessage("'{PropertyName}' must be a non-empty GUID.");

    public static IRuleBuilderOptions<T, int?> MustBeVersion<T>(this IRuleBuilder<T, int?> rule)
        => rule.Must(v => v is null or >= 0).WithMessage("If-Match must contain a document version, for example \"3\".");

    public static IRuleBuilderOptions<T, string?> MustBeEnum<T, TEnum>(this IRuleBuilder<T, string?> rule, params TEnum[] allowed)
        where TEnum : struct, Enum
    {
        var values = allowed.Length > 0 ? allowed : Enum.GetValues<TEnum>();
        return rule
            .Must(v => v is null || (Enum.TryParse<TEnum>(v, true, out var parsed)
                                     && !int.TryParse(v, out _)
                                     && values.Contains(parsed)))
            .WithMessage($"'{{PropertyName}}' must be one of: {string.Join(", ", values)}.");
    }

    public static IRuleBuilderOptions<T, int> MustBePage<T>(this IRuleBuilder<T, int> rule)
        => rule.GreaterThanOrEqualTo(1);

    public static IRuleBuilderOptions<T, int> MustBePageSize<T>(this IRuleBuilder<T, int> rule)
        => rule.InclusiveBetween(1, MaxPageSize);
}
