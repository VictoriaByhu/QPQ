using Catalog.Domain.Constants;
using Catalog.Domain.Exceptions;

namespace Catalog.Domain.Models;

/// <summary>Спільні перевірки інваріантів, щоб сутності не дублювали однакові правила.</summary>
internal static class Guard
{
    public static string Name(string? value, string what, int maxLength = CatalogConstants.NameMaxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{what} must not be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{what} must be at most {maxLength} characters.");
        return trimmed;
    }

    public static string Slug(string? value)
    {
        var slug = Name(value, "Slug", CatalogConstants.SlugMaxLength);
        if (!CatalogConstants.SlugRegex.IsMatch(slug))
            throw new DomainException("Slug may contain only lowercase letters, digits and single hyphens.");
        return slug;
    }

    public static string? Optional(string? value, string what, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{what} must be at most {maxLength} characters.");
        return trimmed;
    }
}
