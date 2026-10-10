using Swaps.Domain.Models;

namespace Swaps.Dal.Interfaces;

public interface ISwapRepository : IGenericRepository<Swap>
{
    Task<Swap?> GetWithDetailsAsync(int swapId, CancellationToken ct = default);

    Task<IReadOnlyList<Swap>> GetForUserAsync(
        Guid userId, string? role, string? status, int page, int pageSize,
        CancellationToken ct = default);

    Task AddDetailsAsync(SwapDetails details, CancellationToken ct = default);

    Task AddSkillAsync(SwapSkill skill, CancellationToken ct = default);

    Task<bool> UpdateDetailsAsync(SwapDetails details, CancellationToken ct = default);

    Task<bool> TouchAsync(int swapId, Guid userId, byte[] rowVersion, CancellationToken ct = default);

    Task<byte[]> ChangeStatusAsync(
        int swapId, string newStatus, Guid userId, byte[] rowVersion, string? comment,
        CancellationToken ct = default);

    Task SoftDeleteAsync(int swapId, Guid userId, byte[] rowVersion, CancellationToken ct = default);
}
