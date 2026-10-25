using MongoDB.Bson;
using Messages.Domain.Constants;
using Messages.Domain.Enums;
using Messages.Domain.Exceptions;
using Messages.Domain.ValueObjects;

namespace Messages.Domain.Entities;

public sealed class SystemMessage : Message
{
    public SystemEvent Event { get; private set; }
    public SystemEventDetails Details { get; private set; } = null!;

    public override string Type => MessageTypes.System;
    public override string Preview => Event.ToString();

    private SystemMessage() { }

    public SystemMessage(ObjectId conversationId, string actorId, SystemEvent systemEvent, int swapId)
        : base(conversationId, actorId)
    {
        if (!Enum.IsDefined(systemEvent))
            throw new DomainException($"Unknown system event '{systemEvent}'.");

        Event = systemEvent;
        Details = new SystemEventDetails(swapId);
    }

    public override void Delete(string userId)
        => throw new DomainException("System messages cannot be deleted.");
}
