using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;

namespace Matching.Infrastructure.Persistence.Queries;

public sealed class SkillStatsQueries(MongoDbContext context) : ISkillStatsQueries
{
    private IMongoCollection<BsonDocument> Collection => context.Raw(CollectionNames.SkillStats);

    public async Task<SkillStatsDto?> GetByIdAsync(int skillId, CancellationToken ct)
        => (await Collection.Find(Builders<BsonDocument>.Filter.Eq("_id", skillId)).FirstOrDefaultAsync(ct))?.ToSkillStatsDto();

    public async Task<IReadOnlyList<SkillStatsDto>> GetAllAsync(CancellationToken ct)
        => (await Collection.Find(FilterDefinition<BsonDocument>.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id")).ToListAsync(ct))
            .Select(d => d.ToSkillStatsDto()).ToList();
}
