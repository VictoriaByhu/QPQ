using Microsoft.EntityFrameworkCore;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Models;

namespace Sessions.Dal.Repositories;

public class AcceptedSwapRepository(SessionsDbContext context)
    : GenericRepository<AcceptedSwap>(context), IAcceptedSwapRepository
{
    public async Task<AcceptedSwap?> GetWithSessionsAsync(int swapId, CancellationToken ct = default)
    {
        var swap = await Set.FirstOrDefaultAsync(a => a.SwapId == swapId, ct);
        if (swap is null)
            return null;

        await Context.Entry(swap)
            .Collection(a => a.Sessions)
            .Query()
            .Include(s => s.Format)
            .LoadAsync(ct);

        return swap;
    }
}
