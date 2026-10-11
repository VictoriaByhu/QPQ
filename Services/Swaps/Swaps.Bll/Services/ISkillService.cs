using Swaps.Bll.Dtos;

namespace Swaps.Bll.Services;

public interface ISkillService
{
    Task<IReadOnlyList<SkillDto>> GetAllAsync(CancellationToken ct = default);
    Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default);
}
