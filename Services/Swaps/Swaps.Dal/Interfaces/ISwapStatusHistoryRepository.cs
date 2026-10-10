using Swaps.Domain.Models;

namespace Swaps.Dal.Interfaces;

public interface ISwapStatusHistoryRepository
{
    Task AddAsync(SwapStatusHistoryEntry entry, CancellationToken ct = default);
    Task<IReadOnlyList<SwapStatusHistoryEntry>> GetBySwapAsync(int swapId, CancellationToken ct = default);
}
