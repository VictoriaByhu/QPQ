using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Application.Common.Interfaces;
using Matching.Application.Common.Models;

namespace Matching.Infrastructure.Persistence.Queries;

/// <summary>
/// Query-сторона: проєкція в БД (лише потрібні поля), фільтрація й пагінація skip/limit виконуються в MongoDB.
/// </summary>
public sealed class UserSkillIndexQueries(MongoDbContext context) : IUserSkillIndexQueries
{
    private static readonly ProjectionDefinition<BsonDocument> Fields = Builders<BsonDocument>.Projection
        .Include("userSkillId").Include("userId").Include("skillId").Include("skillName").Include("type")
        .Include("level").Include("status").Include("experienceYears").Include("languages")
        .Include("availability").Include("version").Include("updatedAt");

    private IMongoCollection<BsonDocument> Collection => context.Raw(CollectionNames.UserSkillIndex);

    public async Task<IndexEntryDto?> GetByIdAsync(string id, CancellationToken ct)
    {
        if (!ObjectId.TryParse(id, out var objectId)) return null;
        var doc = await Collection.Find(Builders<BsonDocument>.Filter.Eq("_id", objectId))
            .Project(Fields).FirstOrDefaultAsync(ct);
        return doc?.ToIndexEntryDto();
    }

    public async Task<PagedResult<IndexEntryDto>> GetListAsync(
        string? userId, int? skillId, string? type, int page, int pageSize, CancellationToken ct)
    {
        var f = Builders<BsonDocument>.Filter;
        var filter = f.Empty;
        if (userId is not null) filter &= f.Eq("userId", userId);
        if (skillId is not null) filter &= f.Eq("skillId", skillId.Value);
        if (type is not null) filter &= f.Eq("type", type);

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);
        var docs = await Collection.Find(filter).Project(Fields)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .Skip((page - 1) * pageSize).Limit(pageSize)
            .ToListAsync(ct);

        return new PagedResult<IndexEntryDto>(docs.Select(d => d.ToIndexEntryDto()).ToList(), page, pageSize, total);
    }
}
