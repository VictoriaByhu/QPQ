using Platform.ServiceDefaults.Metrics;
using Swaps.Bll.Caching;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;
using Swaps.Domain.Constants;

namespace Swaps.Api.Metrics;

/// <summary>Outermost decorator: measures swap operations and counts successful ones; the logic stays in the BLL.</summary>
public sealed class MeteredSwapService(CachedSwapService inner, SwapMetrics metrics) : ISwapService, ISwapCardReader
{
    public async Task<SwapDto> CreateAsync(Guid userId, CreateSwapDto input, CancellationToken ct = default)
    {
        var created = await metrics.Duration.MeasureAsync("create", () => inner.CreateAsync(userId, input, ct));
        metrics.Created(created.Status);
        return created;
    }

    public Task<SwapDto> GetByIdAsync(Guid userId, int swapId, CancellationToken ct = default)
        => metrics.Duration.MeasureAsync("get_card", () => inner.GetByIdAsync(userId, swapId, ct));

    public Task<SwapDto?> GetCardAsync(int swapId, CancellationToken ct = default)
        => metrics.Duration.MeasureAsync("get_card", () => inner.GetCardAsync(swapId, ct));

    public Task AcceptAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync("accept", SwapStatuses.Accepted, () => inner.AcceptAsync(userId, swapId, input, ct));

    public Task RejectAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync("reject", SwapStatuses.Rejected, () => inner.RejectAsync(userId, swapId, input, ct));

    public Task CancelAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync("cancel", SwapStatuses.Cancelled, () => inner.CancelAsync(userId, swapId, input, ct));

    public Task CompleteAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
        => ChangeStatusAsync("complete", SwapStatuses.Completed, () => inner.CompleteAsync(userId, swapId, input, ct));

    public Task<IReadOnlyList<SwapListItemDto>> GetMineAsync(Guid userId, SwapQueryDto query, CancellationToken ct = default)
        => inner.GetMineAsync(userId, query, ct);

    public Task<SwapDto> UpdateDetailsAsync(Guid userId, int swapId, UpdateSwapDetailsDto input, CancellationToken ct = default)
        => inner.UpdateDetailsAsync(userId, swapId, input, ct);

    public Task DeleteAsync(Guid userId, int swapId, CancellationToken ct = default)
        => inner.DeleteAsync(userId, swapId, ct);

    public Task<IReadOnlyList<SwapStatusHistoryDto>> GetHistoryAsync(Guid userId, int swapId, CancellationToken ct = default)
        => inner.GetHistoryAsync(userId, swapId, ct);

    private async Task ChangeStatusAsync(string operation, string newStatus, Func<Task> change)
    {
        await metrics.Duration.MeasureAsync(operation, change);
        metrics.StatusChanged(newStatus);
    }
}
