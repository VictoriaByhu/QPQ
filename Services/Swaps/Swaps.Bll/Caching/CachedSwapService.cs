using AutoMapper;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Exceptions;

namespace Swaps.Bll.Caching;

public sealed class CachedSwapService(
    ISwapService inner,
    IUnitOfWork uow,
    IMapper mapper,
    TwoLevelCache cache) : ISwapService, ISwapCardReader
{
    public Task<SwapDto?> GetCardAsync(int swapId, CancellationToken ct = default)
        => cache.GetOrCreateAsync<SwapDto>(
            CacheKeys.Swap(swapId),
            async token =>
            {
                var swap = await uow.Swaps.GetWithDetailsAsync(swapId, token);
                return swap is null ? null : mapper.Map<SwapDto>(swap);
            },
            CacheTtl.SwapCard,
            ct);

    public async Task<SwapDto> GetByIdAsync(Guid userId, int swapId, CancellationToken ct = default)
    {
        // The card is shared between participants, so access is checked after the read and is never cached.
        var card = await GetCardAsync(swapId, ct);
        if (card is null || (card.InitiatorId != userId && card.PartnerId != userId))
            throw new NotFoundException($"Swap {swapId} not found");

        return card;
    }

    public async Task<SwapDto> CreateAsync(Guid userId, CreateSwapDto input, CancellationToken ct = default)
    {
        var created = await inner.CreateAsync(userId, input, ct);
        await InvalidateAsync(created.Id, "swap created");
        return created;
    }

    public async Task<SwapDto> UpdateDetailsAsync(
        Guid userId, int swapId, UpdateSwapDetailsDto input, CancellationToken ct = default)
    {
        var updated = await inner.UpdateDetailsAsync(userId, swapId, input, ct);
        await InvalidateAsync(swapId, "swap details updated");
        return updated;
    }

    public async Task DeleteAsync(Guid userId, int swapId, CancellationToken ct = default)
    {
        await inner.DeleteAsync(userId, swapId, ct);
        await InvalidateAsync(swapId, "swap deleted");
    }

    public async Task AcceptAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.AcceptAsync(userId, swapId, input, ct);
        await InvalidateAsync(swapId, "swap accepted");
    }

    public async Task RejectAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.RejectAsync(userId, swapId, input, ct);
        await InvalidateAsync(swapId, "swap rejected");
    }

    public async Task CancelAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.CancelAsync(userId, swapId, input, ct);
        await InvalidateAsync(swapId, "swap cancelled");
    }

    public async Task CompleteAsync(Guid userId, int swapId, ChangeSwapStatusDto? input, CancellationToken ct = default)
    {
        await inner.CompleteAsync(userId, swapId, input, ct);
        await InvalidateAsync(swapId, "swap completed");
    }

    public Task<IReadOnlyList<SwapListItemDto>> GetMineAsync(
        Guid userId, SwapQueryDto query, CancellationToken ct = default)
        => inner.GetMineAsync(userId, query, ct);

    public Task<IReadOnlyList<SwapStatusHistoryDto>> GetHistoryAsync(
        Guid userId, int swapId, CancellationToken ct = default)
        => inner.GetHistoryAsync(userId, swapId, ct);

    // The write is already committed here, so the request's token must not be able to skip the invalidation.
    private Task InvalidateAsync(int swapId, string reason)
        => cache.InvalidateAsync(reason, CancellationToken.None, CacheKeys.Swap(swapId));
}
