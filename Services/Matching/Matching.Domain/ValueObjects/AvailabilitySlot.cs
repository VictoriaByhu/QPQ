using Matching.Domain.Common;
using Matching.Domain.Exceptions;

namespace Matching.Domain.ValueObjects;

/// <summary>Вікно доступності: день тижня 1..7 (пн..нд) та час "HH:mm" (як у документі userSkillIndex).</summary>
public sealed class AvailabilitySlot : ValueObject
{
    public int DayOfWeek { get; }
    public string Start { get; }
    public string End { get; }

    public AvailabilitySlot(int dayOfWeek, string start, string end)
    {
        DayOfWeek = Guard.InRange(dayOfWeek, 1, 7, "DayOfWeek");
        if (!TimeOnly.TryParseExact(start, "HH:mm", out var from))
            throw new DomainException("Start must be in HH:mm format");
        if (!TimeOnly.TryParseExact(end, "HH:mm", out var to))
            throw new DomainException("End must be in HH:mm format");
        if (from >= to)
            throw new DomainException("Start must be earlier than End");
        Start = start;
        End = end;
    }

    /// <summary>Вікна перетинаються, якщо вони в той самий день і мають спільний інтервал.</summary>
    public bool Overlaps(AvailabilitySlot other)
        => DayOfWeek == other.DayOfWeek
           && string.CompareOrdinal(Start, other.End) < 0
           && string.CompareOrdinal(other.Start, End) < 0;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return DayOfWeek;
        yield return Start;
        yield return End;
    }
}
