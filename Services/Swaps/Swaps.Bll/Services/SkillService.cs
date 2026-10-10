using AutoMapper;
using Swaps.Bll.Dtos;
using Swaps.Dal.Interfaces;
using Swaps.Domain.Exceptions;

namespace Swaps.Bll.Services;

public sealed class SkillService(IUnitOfWork uow, IMapper mapper) : ISkillService
{
    public async Task<IReadOnlyList<SkillDto>> GetAllAsync(CancellationToken ct = default)
    {
        var skills = await uow.Skills.GetAllAsync(ct);
        return mapper.Map<List<SkillDto>>(skills);
    }

    public async Task<SkillDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException($"Skill {id} not found");
        return mapper.Map<SkillDto>(skill);
    }
}
