using Microsoft.EntityFrameworkCore;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Models;

namespace Sessions.Dal.Repositories;

public class SessionSkillRepository(SessionsDbContext context)
    : GenericRepository<SessionSkill>(context), ISessionSkillRepository
{
    public async Task<IReadOnlyList<SessionSkill>> GetBySkillForUserAsync(
        int skillId, Guid userId, DateTime? from, CancellationToken ct = default)
    {
        var query = Set
            .Include(ss => ss.Session).ThenInclude(s => s.Format)
            .Where(ss => ss.SkillId == skillId
                         && (ss.Session.Swap.InitiatorId == userId || ss.Session.Swap.PartnerId == userId));

        if (from is not null)
            query = query.Where(ss => ss.Session.ScheduledAt >= from.Value);

        return await query
            .OrderBy(ss => ss.Session.ScheduledAt)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
