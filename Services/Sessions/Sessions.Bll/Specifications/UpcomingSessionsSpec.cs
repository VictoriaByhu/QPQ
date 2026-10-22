using Ardalis.Specification;
using Sessions.Domain.Constants;
using Sessions.Domain.Models;

namespace Sessions.Bll.Specifications;

public sealed class UpcomingSessionsSpec : Specification<Session>
{
    public UpcomingSessionsSpec(Guid userId, DateTime from, int take)
    {
        Query.Include(s => s.Format)
            .Where(s => s.Status == SessionStatuses.Planned
                        && s.ScheduledAt >= from
                        && (s.Swap.InitiatorId == userId || s.Swap.PartnerId == userId))
            .OrderBy(s => s.ScheduledAt)
            .Take(take)
            .AsNoTracking();
    }
}
