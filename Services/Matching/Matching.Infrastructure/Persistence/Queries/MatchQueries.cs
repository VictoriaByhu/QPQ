using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;

namespace Matching.Infrastructure.Persistence.Queries;

public sealed class MatchQueries(MongoDbContext context) : IMatchQueries
{
    private IMongoCollection<BsonDocument> Collection => context.Raw(CollectionNames.Matches);

    public async Task<MatchDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null;
        var doc = await Collection.Find(Builders<BsonDocument>.Filter.Eq("_id", objectId)).FirstOrDefaultAsync(ct);
        return doc?.ToMatchDto();
    }

    public async Task<PagedResult<MatchDto>> GetForUserAsync(
        string userId, bool includeDismissed, int page, int pageSize, CancellationToken ct)
    {
        // Запит покривається індексом ix_matches_user_status_score { userIds, status, score: -1 }.
        var f = Builders<BsonDocument>.Filter;
        var filter = f.Eq("userIds", userId);
        if (!includeDismissed) filter &= f.Ne("status", "Dismissed");

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var docs = await Collection.Find(filter)
            .Sort(Builders<BsonDocument>.Sort.Descending("score").Descending("_id"))
            .Skip((page - 1) * pageSize).Limit(pageSize)
            .ToListAsync(ct);

        return new PagedResult<MatchDto>(docs.Select(d => d.ToMatchDto()).ToList(), page, pageSize, total);
    }
}
