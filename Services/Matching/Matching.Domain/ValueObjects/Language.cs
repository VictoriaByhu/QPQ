using Matching.Domain.Common;
using Matching.Domain.Exceptions;

namespace Matching.Domain.ValueObjects;

/// <summary>Мова викладання/навчання: код і рівень володіння 1..5.</summary>
public sealed class Language : ValueObject
{
    public string Code { get; }
    public int Proficiency { get; }

    public Language(string code, int proficiency)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Trim().Length is < 2 or > 10)
            throw new DomainException("Language code must be 2-10 characters");
        Code = code.Trim().ToLowerInvariant();
        Proficiency = Guard.InRange(proficiency, 1, 5, "Language proficiency");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Proficiency;
    }
}
