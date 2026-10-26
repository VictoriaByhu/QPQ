using Matching.Domain.Exceptions;

namespace Matching.Domain.Common;

/// <summary>Перевірки інваріантів, спільні для сутностей і Value Objects.</summary>
internal static class Guard
{
    public static string UserId(string? value, string name)
    {
        if (!IsUserId(value))
            throw new DomainException($"{name} must be a GUID in the form 00000000-0000-0000-0000-000000000000");
        return value!;
    }

    /// <summary>Формат збігається з шаблоном <c>$jsonSchema</c> у db/p3/matching.collections.js.</summary>
    public static bool IsUserId(string? value) => value is not null && Guid.TryParseExact(value, "D", out _);

    public static int InRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
            throw new DomainException($"{name} must be between {min} and {max}");
        return value;
    }

    public static int Positive(int value, string name)
    {
        if (value < 1) throw new DomainException($"{name} must be greater than 0");
        return value;
    }

    public static string Text(string? value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must be at most {maxLength} characters");
        return trimmed;
    }
}
