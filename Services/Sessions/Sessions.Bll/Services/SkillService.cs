using AutoMapper;
using Sessions.Bll.Dtos;
using Sessions.Dal.Interfaces;
using Sessions.Domain.Exceptions;

namespace Sessions.Bll.Services;

public sealed class SkillService(IUnitOfWork uow, IMapper mapper) : ISkillService
{
    public async Task<IReadOnlyList<SkillDto>> GetAllAsync(CancellationToken ct = default)
    {
        var skills = await uow.Skills.GetAllAsync(ct);
        return mapper.Map<List<SkillDto>>(skills.Where(s => !s.IsDeleted).OrderBy(s => s.Id));
    }

    public async Task<IReadOnlyList<SkillSessionDto>> GetSessionsAsync(
        Guid userId, int skillId, DateTime? from, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetByIdAsync(skillId, ct);
        if (skill is null || skill.IsDeleted)
            throw new NotFoundException($"Skill {skillId} not found");

        var sessionSkills = await uow.SessionSkills.GetBySkillForUserAsync(skillId, userId, from, ct);
        return mapper.Map<List<SkillSessionDto>>(sessionSkills);
    }
}
