using FluentValidation;
using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Exceptions;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;

namespace Matching.Application.SkillStats.Queries.GetSkillStats;

public sealed record GetSkillStatsQuery(int SkillId) : IQuery<SkillStatsDto>;

public sealed class GetSkillStatsQueryValidator : AbstractValidator<GetSkillStatsQuery>
{
    public GetSkillStatsQueryValidator() => RuleFor(x => x.SkillId).GreaterThan(0);
}

public sealed class GetSkillStatsQueryHandler(ISkillStatsQueries queries)
    : IRequestHandler<GetSkillStatsQuery, SkillStatsDto>
{
    public async Task<SkillStatsDto> Handle(GetSkillStatsQuery q, CancellationToken ct)
        => await queries.GetByIdAsync(q.SkillId, ct) ?? throw new NotFoundException("Skill stats", q.SkillId.ToString());
}
