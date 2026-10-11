using AutoMapper;
using UserSkills.Bll.Dtos;
using UserSkills.Dal.Interfaces;
using UserSkills.Domain.Exceptions;

namespace UserSkills.Bll.Services;

public sealed class ReferenceDataService(IUnitOfWork uow, IMapper mapper) : IReferenceDataService
{
    public async Task<IReadOnlyList<SkillDto>> GetSkillsAsync(CancellationToken ct = default)
        => mapper.Map<IReadOnlyList<SkillDto>>(await uow.Skills.GetAllAsync(ct));

    public async Task<SkillDto> GetSkillAsync(int id, CancellationToken ct = default)
    {
        var skill = await uow.Skills.GetByIdAsync(id, ct)
                    ?? throw new NotFoundException($"Skill {id} not found");
        return mapper.Map<SkillDto>(skill);
    }

    public async Task<IReadOnlyList<LanguageRefDto>> GetLanguagesAsync(CancellationToken ct = default)
        => mapper.Map<IReadOnlyList<LanguageRefDto>>(await uow.Languages.GetAllAsync(ct));

    public async Task<LanguageRefDto> GetLanguageAsync(string code, CancellationToken ct = default)
    {
        var language = await uow.Languages.GetByCodeAsync(code, ct)
                       ?? throw new NotFoundException($"Language '{code}' not found");
        return mapper.Map<LanguageRefDto>(language);
    }
}
