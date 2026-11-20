using Messages.Domain.Entities;
using Messages.Domain.Enums;
using Messages.Infrastructure.Persistence;
using MongoDB.Driver;
using Platform.EventBus;

namespace Messages.WebApi.IntegrationEvents;

/// <summary>
/// Keeps the swap conversation in sync with Swaps. MongoDB runs as a single node here, without transactions,
/// so every step is idempotent on its own and processed_events is written last:
/// <list type="number">
/// <item>the conversation is created once per swap (unique index on swapId), with the SwapAccepted system message;</item>
/// <item>the status is set only if it differs and the event is newer than swapStatusUpdatedAt, so a late event never rolls it back;</item>
/// <item>the eventId goes into processed_events; a duplicate key means another delivery already finished.</item>
/// </list>
/// A failure between the steps leads to a retry that repeats only the steps not yet done.
/// </summary>
public sealed class ConversationReplicator(MongoDbContext context, ILogger<ConversationReplicator> logger)
{
    public async Task ApplyAsync(
        Guid eventId, string eventType, int swapId, Guid initiatorId, Guid partnerId,
        string status, DateTime changedAt, Guid changedByUserId, CancellationToken ct)
    {
        var id = eventId.ToString("D");
        if (await context.ProcessedEvents.Find(e => e.EventId == id).AnyAsync(ct))
        {
            logger.LogInformation("Duplicate event {EventId} skipped: {EventType} for swap {SwapId}", eventId, eventType, swapId);
            return;
        }

        if (!Enum.TryParse<SwapStatus>(status, ignoreCase: false, out var swapStatus))
            throw new PermanentEventException($"Unknown swap status '{status}'.");

        // Any event creates the conversation if it is missing, so the order of arrival does not matter.
        var conversation = await EnsureConversationAsync(swapId, initiatorId, partnerId, ct);

        if (swapStatus != SwapStatus.Accepted)
            await ApplyStatusAsync(conversation, swapStatus, changedAt, changedByUserId, eventType, eventId, ct);

        try
        {
            await context.ProcessedEvents.InsertOneAsync(
                new ProcessedEvent { EventId = id, EventType = eventType, ProcessedAt = DateTime.UtcNow },
                cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            logger.LogInformation("Duplicate event {EventId} skipped: {EventType} for swap {SwapId}", eventId, eventType, swapId);
        }
    }

    private async Task<Conversation> EnsureConversationAsync(int swapId, Guid initiatorId, Guid partnerId, CancellationToken ct)
    {
        var existing = await context.Conversations.Find(c => c.SwapId == swapId).FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing;

        var conversation = new Conversation(swapId, initiatorId.ToString("D"), partnerId.ToString("D"));
        try
        {
            await context.Conversations.InsertOneAsync(conversation, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // Created in the meantime (for example, through the REST API); use that one.
            return await context.Conversations.Find(c => c.SwapId == swapId).FirstAsync(ct);
        }

        // Only the partner can accept a swap, so the opening system message is theirs, as with the REST API.
        await context.Messages.InsertOneAsync(conversation.CreateStatusMessage(partnerId.ToString("D")), cancellationToken: ct);
        logger.LogInformation("Created conversation {ConversationId} for swap {SwapId}", conversation.Id.ToString(), swapId);
        return conversation;
    }

    private async Task ApplyStatusAsync(
        Conversation conversation, SwapStatus status, DateTime changedAt, Guid changedByUserId,
        string eventType, Guid eventId, CancellationToken ct)
    {
        var filter = Builders<Conversation>.Filter;
        var update = await context.Conversations.UpdateManyAsync(
            filter.Eq(c => c.SwapId, conversation.SwapId)
            & filter.Ne(c => c.SwapStatus, status)
            & (filter.Eq(c => c.SwapStatusUpdatedAt, null) | filter.Lt(c => c.SwapStatusUpdatedAt, changedAt)),
            Builders<Conversation>.Update
                .Set(c => c.SwapStatus, status)
                .Set(c => c.SwapStatusUpdatedAt, changedAt)
                .Set(c => c.UpdatedAt, DateTime.UtcNow)
                .Inc(c => c.Version, 1),
            cancellationToken: ct);

        if (update.ModifiedCount == 0)
        {
            logger.LogInformation("Stale or repeated {EventType} {EventId} ignored: swap {SwapId} already has this or a newer status",
                eventType, eventId, conversation.SwapId);
            return;
        }

        var actor = conversation.HasParticipant(changedByUserId.ToString("D"))
            ? changedByUserId.ToString("D")
            : conversation.Participants.First(p => p.Role == ParticipantRole.Partner).UserId;
        var systemEvent = status == SwapStatus.Completed ? SystemEvent.SwapCompleted : SystemEvent.SwapCancelled;
        await context.Messages.InsertOneAsync(
            new SystemMessage(conversation.Id, actor, systemEvent, conversation.SwapId), cancellationToken: ct);

        logger.LogInformation("Replicated {EventType} {EventId}: swap {SwapId} conversation is {Status} as of {ChangedAt}",
            eventType, eventId, conversation.SwapId, status, changedAt);
    }
}
