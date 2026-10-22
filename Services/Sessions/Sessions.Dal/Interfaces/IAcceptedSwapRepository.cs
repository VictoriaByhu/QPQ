using Sessions.Domain.Models;

namespace Sessions.Dal.Interfaces;

public interface IAcceptedSwapRepository : IGenericRepository<AcceptedSwap>
{
    Task<AcceptedSwap?> GetWithSessionsAsync(int swapId, CancellationToken ct = default);
}
