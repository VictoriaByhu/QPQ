using MongoDB.Bson;
using Messages.Domain.Common;
using Messages.Domain.Enums;
using Messages.Domain.Exceptions;
using Messages.Domain.ValueObjects;

namespace Messages.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    private List<Participant> _participants = [];

    public int SwapId { get; private set; }
    public SwapStatus SwapStatus { get; private set; }
    public LastMessage? LastMessage { get; private set; }
    public IReadOnlyCollection<Participant> Participants => _participants.AsReadOnly();

    private Conversation() { }

    public Conversation(int swapId, string initiatorId, string partnerId)
    {
        if (swapId <= 0)
            throw new DomainException("SwapId must be positive.");

        var initiator = new Participant(initiatorId, ParticipantRole.Initiator, null);
        var partner = new Participant(partnerId, ParticipantRole.Partner, null);
        if (initiator.UserId == partner.UserId)
            throw new DomainException("Initiator and partner must be different users.");

        SwapId = swapId;
        SwapStatus = SwapStatus.Accepted;
        _participants.Add(initiator);
        _participants.Add(partner);
    }

    public bool HasParticipant(string userId)
        => Guid.TryParse(userId, out var id) && _participants.Any(p => p.UserId == id.ToString("D"));

    public SystemMessage CreateStatusMessage(string actorId)
    {
        EnsureParticipant(actorId);
        var systemEvent = SwapStatus switch
        {
            SwapStatus.Accepted => SystemEvent.SwapAccepted,
            SwapStatus.Completed => SystemEvent.SwapCompleted,
            _ => SystemEvent.SwapCancelled
        };
        return new SystemMessage(Id, actorId, systemEvent, SwapId);
    }

    public SystemMessage ChangeSwapStatus(SwapStatus status, string actorId)
    {
        EnsureParticipant(actorId);
        if (SwapStatus != SwapStatus.Accepted)
            throw new DomainException($"The swap is already {SwapStatus}, its status cannot be changed.");
        if (status == SwapStatus.Accepted)
            throw new DomainException("The swap is already Accepted.");

        SwapStatus = status;
        Touch();
        return CreateStatusMessage(actorId);
    }

    public void RegisterMessage(Message message)
    {
        if (message.ConversationId != Id)
            throw new DomainException("The message belongs to another conversation.");
        EnsureParticipant(message.SenderId);
        if (message is SystemMessage)
            return;
        EnsureAcceptsContent();

        LastMessage = Snapshot(message);
        Touch();
    }

    public void EnsureAcceptsContent()
    {
        if (SwapStatus == SwapStatus.Cancelled)
            throw new DomainException("The swap was cancelled, the conversation is read-only.");
    }

    public bool SyncLastMessage(ObjectId changedMessageId, Message? latestVisible)
    {
        if (LastMessage?.MessageId != changedMessageId)
            return false;

        LastMessage = latestVisible is null || latestVisible.IsDeleted ? null : Snapshot(latestVisible);
        Touch();
        return true;
    }

    public void MarkRead(string userId, DateTime readAt)
    {
        var index = IndexOf(userId);
        _participants[index] = _participants[index].ReadAt(readAt);
        Touch();
    }

    public void EnsureCanBeDeleted()
    {
        if (SwapStatus == SwapStatus.Accepted)
            throw new DomainException("A conversation of an active swap cannot be deleted.");
    }

    private void EnsureParticipant(string userId) => IndexOf(userId);

    private int IndexOf(string userId)
    {
        var index = Guid.TryParse(userId, out var id)
            ? _participants.FindIndex(p => p.UserId == id.ToString("D"))
            : -1;
        if (index < 0)
            throw new DomainException("The user is not a participant of this conversation.");
        return index;
    }

    private static LastMessage Snapshot(Message message)
        => new(message.Id, message.SenderId, LastMessage.Shorten(message.Preview), message.CreatedAt);
}
