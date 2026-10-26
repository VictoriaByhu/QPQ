using MongoDB.Bson;
using Matching.Domain.Common;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Domain.ValueObjects;

namespace Matching.Domain.Entities;

/// <summary>
/// Знайдена пара (колекція matches). <see cref="MatchKind.OneWay"/> містить одну пропозицію,
/// <see cref="MatchKind.Mutual"/> — ще й зустрічну (<see cref="CounterOffer"/>).
/// Пари тимчасові: документ видаляє TTL-індекс за <see cref="ExpiresAt"/>.
/// </summary>
public sealed class Match : BaseEntity
{
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(90);

    private List<string> _userIds = new();
    private List<string> _commonLanguages = new();

    public MatchKind Kind { get; private set; }
    public MatchStatus Status { get; private set; }
    public int Score { get; private set; }
    public IReadOnlyList<string> UserIds => _userIds.AsReadOnly();
    public MatchOffer Offer { get; private set; } = null!;
    public MatchOffer? CounterOffer { get; private set; }
    public IReadOnlyList<string> CommonLanguages => _commonLanguages.AsReadOnly();
    public DateTime ExpiresAt { get; private set; }

    private Match() { }                                       // для десеріалізації

    private Match(
        MatchKind kind, MatchOffer offer, MatchOffer? counterOffer, int score,
        IEnumerable<string> commonLanguages, MatchStatus status, DateTime? expiresAt,
        ObjectId? id, DateTime? createdAt) : base(id, createdAt)
    {
        if (kind == MatchKind.Mutual && counterOffer is null)
            throw new DomainException("A mutual match requires a counter offer");
        if (counterOffer is not null && !IsReverse(offer, counterOffer))
            throw new DomainException("Counter offer must go in the opposite direction of the offer");
        if (!Enum.IsDefined(status)) throw new DomainException("Unknown status");

        Kind = kind;
        Status = status;
        Offer = offer;
        CounterOffer = counterOffer;
        Score = Guard.InRange(score, 0, 100, "Score");
        _commonLanguages = NormalizeLanguages(commonLanguages);
        _userIds = new[] { offer.FromUserId, offer.ToUserId }.OrderBy(x => x, StringComparer.Ordinal).ToList();
        ExpiresAt = expiresAt ?? CreatedAt + DefaultLifetime;
        if (ExpiresAt <= CreatedAt) throw new DomainException("ExpiresAt must be later than CreatedAt");
    }

    public static Match OneWay(
        MatchOffer offer, int score, IEnumerable<string> commonLanguages,
        MatchStatus status = MatchStatus.Suggested, DateTime? expiresAt = null,
        ObjectId? id = null, DateTime? createdAt = null)
        => new(MatchKind.OneWay, offer, null, score, commonLanguages, status, expiresAt, id, createdAt);

    public static Match Mutual(
        MatchOffer offer, MatchOffer counterOffer, int score, IEnumerable<string> commonLanguages,
        MatchStatus status = MatchStatus.Suggested, DateTime? expiresAt = null,
        ObjectId? id = null, DateTime? createdAt = null)
        => new(MatchKind.Mutual, offer, counterOffer, score, commonLanguages, status, expiresAt, id, createdAt);

    /// <summary>Допустимі переходи: Suggested -> Viewed | Dismissed; Viewed -> Dismissed; Dismissed — кінцевий.</summary>
    public void ChangeStatus(MatchStatus newStatus)
    {
        if (!Enum.IsDefined(newStatus)) throw new DomainException("Unknown status");
        var allowed = (Status, newStatus) switch
        {
            (MatchStatus.Suggested, MatchStatus.Viewed) => true,
            (MatchStatus.Suggested, MatchStatus.Dismissed) => true,
            (MatchStatus.Viewed, MatchStatus.Dismissed) => true,
            _ => false
        };
        if (!allowed) throw new DomainException($"Cannot change match status from {Status} to {newStatus}");
        Status = newStatus;
        Touch();
    }

    /// <summary>Одностороння пара стала взаємною, бо знайшлася зустрічна пропозиція.</summary>
    public void UpgradeToMutual(MatchOffer counterOffer, int score)
    {
        if (Kind == MatchKind.Mutual) throw new DomainException("Match is already mutual");
        if (!IsReverse(Offer, counterOffer))
            throw new DomainException("Counter offer must go in the opposite direction of the offer");
        Kind = MatchKind.Mutual;
        CounterOffer = counterOffer;
        Score = Guard.InRange(score, 0, 100, "Score");
        Touch();
    }

    private static bool IsReverse(MatchOffer offer, MatchOffer counter)
        => offer.FromUserId == counter.ToUserId && offer.ToUserId == counter.FromUserId;

    private static List<string> NormalizeLanguages(IEnumerable<string> codes)
    {
        var list = codes.Select(c => c.Trim().ToLowerInvariant()).Distinct().ToList();
        if (list.Count > 10) throw new DomainException("At most 10 common languages are allowed");
        if (list.Any(c => c.Length is < 2 or > 10)) throw new DomainException("Language code must be 2-10 characters");
        return list;
    }
}
