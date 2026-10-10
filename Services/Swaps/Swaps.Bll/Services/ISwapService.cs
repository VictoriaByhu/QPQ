using Swaps.Bll.Dtos;

namespace Swaps.Bll.Services;

public interface ISwapService
{
    Task<SwapDto> CreateAsync(Guid userId, CreateSwapDto input, CancellationToken ct = default);
    Task<SwapDto> GetByIdAsync(Guid userId, int swapId, CancellationToken ct = default);
    Task<IReadOnlyList<SwapListItemDto>> GetMineAsync(Guid userId, SwapQueryDto query, CancellationToken ct = default);
    Task<SwapDto> UpdateDetailsAsync(Guid userId, int swapId, UpdateSwapDetailsDto input, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, int swapId, CancellationToken ct = default);
    Task AcceptAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default);
    Task RejectAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default);
    Task CancelAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default);
    Task CompleteAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default);
    Task<IReadOnlyList<SwapStatusHistoryDto>> GetHistoryAsync(Guid userId, int swapId, CancellationToken ct = default);
}
