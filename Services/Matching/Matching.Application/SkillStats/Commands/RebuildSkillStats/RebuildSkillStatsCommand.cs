using MediatR;
using Matching.Application.Common;
using Matching.Domain.Interfaces;

namespace Matching.Application.SkillStats.Commands.RebuildSkillStats;

/// <summary>Перераховує похідну колекцію skillStats (aggregation $merge). Повертає кількість документів.</summary>
public sealed record RebuildSkillStatsCommand : ICommand<int>;

public sealed class RebuildSkillStatsCommandHandler(ISkillStatsRepository stats)
    : IRequestHandler<RebuildSkillStatsCommand, int>
{
    public Task<int> Handle(RebuildSkillStatsCommand cmd, CancellationToken ct) => stats.RebuildAsync(ct);
}
