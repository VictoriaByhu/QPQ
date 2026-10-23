using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Dal.Interfaces;
using QPQ.Moderation.Domain.Entities;
using QPQ.Moderation.Domain;

namespace QPQ.Moderation.Dal.Repositories;

public class ModeratorRepository : Repository<Moderator>, IModeratorRepository
{
    public ModeratorRepository(AppDbContext context) : base(context) { }

    public async Task<Moderator?> GetModeratorWithAssignmentsAsync(int id, CancellationToken ct = default)
    {
        var moderator = await _dbSet.FirstOrDefaultAsync(m => m.Id == id, ct);

        if (moderator != null)
        {
            await _context.Entry(moderator)
                .Collection(m => m.Assignments)
                .LoadAsync(ct);
        }

        return moderator;
    }
}