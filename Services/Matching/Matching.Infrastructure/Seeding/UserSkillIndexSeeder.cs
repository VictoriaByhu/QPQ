using MongoDB.Driver;
using Matching.Infrastructure.Persistence;

namespace Matching.Infrastructure.Seeding;

public sealed class UserSkillIndexSeeder(MongoDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.UserSkillIndex.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;                                              // уже наповнено: дублів не буде

        await context.UserSkillIndex.InsertManyAsync(SeedData.IndexEntries(), cancellationToken: ct);
    }
}

public sealed class MatchSeeder(MongoDbContext context) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.Matches.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;

        await context.Matches.InsertManyAsync(SeedData.Matches(), cancellationToken: ct);
    }
}

/// <summary>skillStats — похідні дані: наповнюється агрегацією, а не вручну.</summary>
public sealed class SkillStatsSeeder(MongoDbContext context, Matching.Domain.Interfaces.ISkillStatsRepository stats) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await context.SkillStats.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;

        await stats.RebuildAsync(ct);
    }
}
