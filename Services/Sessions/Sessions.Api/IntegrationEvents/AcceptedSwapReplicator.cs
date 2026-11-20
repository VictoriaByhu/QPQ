using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Sessions.Dal;
using Sessions.Dal.Configurations;
using Sessions.Dal.Entities;
using Sessions.Domain.Models;

namespace Sessions.Api.IntegrationEvents;

/// <summary>
/// Keeps the AcceptedSwaps copy in sync with Swaps. The copy change and the ProcessedEvents row are saved by one
/// SaveChanges, i.e. in one transaction: either the event is applied and remembered, or neither happens.
/// </summary>
public sealed class AcceptedSwapReplicator(SessionsDbContext db, ILogger<AcceptedSwapReplicator> logger)
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task ApplyAsync(
        Guid eventId, string eventType, int swapId, Guid initiatorId, Guid partnerId,
        string status, DateTime changedAt, CancellationToken ct)
    {
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == eventId, ct))
        {
            logger.LogInformation("Duplicate event {EventId} skipped: {EventType} for swap {SwapId}", eventId, eventType, swapId);
            return;
        }

        // Any event creates the copy if it is missing, so the order of arrival does not matter.
        var swap = await db.AcceptedSwaps.SingleOrDefaultAsync(a => a.SwapId == swapId, ct);
        if (swap is null)
        {
            swap = new AcceptedSwap(swapId, initiatorId, partnerId, status);
            db.AcceptedSwaps.Add(swap);
        }

        var applied = swap.ApplyReplicatedStatus(status, changedAt);

        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, EventType = eventType, ProcessedAt = DateTime.UtcNow });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsProcessedEventDuplicate(ex))
        {
            // Another delivery of the same event won the race; its changes are already saved.
            logger.LogInformation("Duplicate event {EventId} skipped: {EventType} for swap {SwapId}", eventId, eventType, swapId);
            return;
        }

        if (applied)
            logger.LogInformation("Replicated {EventType} {EventId}: swap {SwapId} is {Status} as of {ChangedAt}",
                eventType, eventId, swapId, status, changedAt);
        else
            logger.LogInformation("Stale {EventType} {EventId} ignored: swap {SwapId} already has a change from {AppliedAt}",
                eventType, eventId, swapId, swap.StatusChangedAt);
    }

    // Other unique violations (for example, the swap row inserted concurrently) stay transient and are retried.
    private static bool IsProcessedEventDuplicate(DbUpdateException ex)
        => ex.InnerException is SqlException { Number: UniqueConstraintViolation or UniqueIndexViolation } sql
           && sql.Message.Contains(ProcessedEventConfiguration.PrimaryKeyName, StringComparison.Ordinal);
}
