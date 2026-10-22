using AutoMapper;
using Sessions.Bll.Dtos;
using Sessions.Bll.Specifications;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Constants;
using Sessions.Domain.Exceptions;

namespace Sessions.Bll.Services;

public sealed class AcceptedSwapService(IUnitOfWork uow, IMapper mapper) : IAcceptedSwapService
{
    public async Task<IReadOnlyList<AcceptedSwapSummaryDto>> GetMineAsync(
        Guid userId, string? status, CancellationToken ct = default)
    {
        if (status is not null && !AcceptedSwapStatuses.All.Contains(status))
            throw new ValidationException($"Status must be one of: {string.Join(", ", AcceptedSwapStatuses.All)}.");

        var swaps = await uow.AcceptedSwaps.ListAsync(new AcceptedSwapsOfUserSpec(userId, status), ct);
        return mapper.Map<List<AcceptedSwapSummaryDto>>(swaps);
    }

    public async Task<AcceptedSwapDto> GetWithSessionsAsync(Guid userId, int swapId, CancellationToken ct = default)
    {
        var swap = await uow.AcceptedSwaps.GetWithSessionsAsync(swapId, ct);
        if (swap is null || !swap.IsParticipant(userId))
            throw new NotFoundException($"Swap {swapId} not found");

        return mapper.Map<AcceptedSwapDto>(swap);
    }
}
