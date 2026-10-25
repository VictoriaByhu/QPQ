using Messages.Domain.Common;
using Messages.Domain.Enums;
using Messages.Domain.Exceptions;

namespace Messages.Domain.ValueObjects;

public sealed class Participant : ValueObject
{
    public string UserId { get; }
    public ParticipantRole Role { get; }
    public DateTime? LastReadAt { get; }

    public Participant(string userId, ParticipantRole role, DateTime? lastReadAt)
    {
        if (!Enum.IsDefined(role))
            throw new DomainException($"Unknown participant role '{role}'.");

        UserId = Guard.UserId(userId, nameof(UserId));
        Role = role;
        LastReadAt = lastReadAt;
    }

    public Participant ReadAt(DateTime readAt)
    {
        if (LastReadAt >= readAt)
            return this;
        return new Participant(UserId, Role, readAt);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Role;
        yield return LastReadAt;
    }
}
