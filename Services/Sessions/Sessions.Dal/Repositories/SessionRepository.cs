using Microsoft.EntityFrameworkCore;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Dal.Repositories;

public class SessionRepository(SessionsDbContext context)
    : GenericRepository<Session>(context), ISessionRepository
{
    public Task<Session?> GetWithDetailsAsync(int id, CancellationToken ct = default)
        => Set.Include(s => s.Swap)
              .Include(s => s.Format)
              .Include(s => s.Outcome)
              .Include(s => s.Skills).ThenInclude(ss => ss.Skill)
              .FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> HasOverlapAsync(
        int swapId, DateTime scheduledAt, int durationMinutes, int? exceptSessionId, CancellationToken ct = default)
    {
        var endsAt = scheduledAt.AddMinutes(durationMinutes);
        return Set.AnyAsync(s =>
            s.SwapId == swapId
            && s.Status == SessionStatuses.Planned
            && (exceptSessionId == null || s.Id != exceptSessionId)
            && s.ScheduledAt < endsAt
            && s.ScheduledAt.AddMinutes(s.DurationMinutes) > scheduledAt, ct);
    }

    public void SetOriginalRowVersion(Session session, byte[] rowVersion)
        => Context.Entry(session).Property(s => s.RowVersion).OriginalValue = rowVersion;
}
