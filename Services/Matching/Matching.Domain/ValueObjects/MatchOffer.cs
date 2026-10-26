using MongoDB.Bson;
using Matching.Domain.Common;
using Matching.Domain.Exceptions;

namespace Matching.Domain.ValueObjects;

/// <summary>
/// Пропозиція всередині пари: хто кому пропонує яку навичку.
/// Гібрид посилання й знімка: <see cref="IndexRef"/> вказує на запис userSkillIndex,
/// <see cref="SkillName"/> — копія назви на момент підбору.
/// </summary>
public sealed class MatchOffer : ValueObject
{
    public string FromUserId { get; }
    public string ToUserId { get; }
    public int SkillId { get; }
    public string SkillName { get; }
    public ObjectId IndexRef { get; }

    public MatchOffer(string fromUserId, string toUserId, int skillId, string skillName, ObjectId indexRef)
    {
        FromUserId = Guard.UserId(fromUserId, "FromUserId");
        ToUserId = Guard.UserId(toUserId, "ToUserId");
        if (string.Equals(FromUserId, ToUserId, StringComparison.OrdinalIgnoreCase))
            throw new DomainException("A user cannot be matched with himself");
        SkillId = Guard.Positive(skillId, "SkillId");
        SkillName = Guard.Text(skillName, 100, "SkillName");
        IndexRef = indexRef;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FromUserId;
        yield return ToUserId;
        yield return SkillId;
        yield return SkillName;
        yield return IndexRef;
    }
}
