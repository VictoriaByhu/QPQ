using MediatR;
using Matching.Application.Common;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;

using Matching.Application.Caching;
namespace Matching.Application.SkillStats.Queries.GetSkillStatsList;

[Cacheable(CacheGroups.SkillStats)]
public sealed record GetSkillStatsListQuery : IQuery<IReadOnlyList<SkillStatsDto>>;

public sealed class GetSkillStatsListQueryHandler(ISkillStatsQueries queries)
    : IRequestHandler<GetSkillStatsListQuery, IReadOnlyList<SkillStatsDto>>
{
    public Task<IReadOnlyList<SkillStatsDto>> Handle(GetSkillStatsListQuery q, CancellationToken ct)
        => queries.GetAllAsync(ct);
}
