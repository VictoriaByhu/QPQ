using Sessions.Bll.Dtos;

namespace Sessions.Bll.Services;

public interface IAcceptedSwapService
{
    Task<IReadOnlyList<AcceptedSwapSummaryDto>> GetMineAsync(Guid userId, string? status, CancellationToken ct = default);
    Task<AcceptedSwapDto> GetWithSessionsAsync(Guid userId, int swapId, CancellationToken ct = default);
}
