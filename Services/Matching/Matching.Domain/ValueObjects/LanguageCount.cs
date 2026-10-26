using Matching.Domain.Common;
using Matching.Domain.Exceptions;

namespace Matching.Domain.ValueObjects;

/// <summary>Мова та кількість активних записів, які її використовують (для skillStats.topLanguages).</summary>
public sealed class LanguageCount : ValueObject
{
    public string Code { get; }
    public int Count { get; }

    public LanguageCount(string code, int count)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Language code is required");
        Code = code;
        Count = Guard.Positive(count, "Count");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Code;
        yield return Count;
    }
}
