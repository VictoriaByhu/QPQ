using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Domain.Interfaces;

namespace Matching.Infrastructure.Persistence.Repositories;

public sealed class SkillStatsRepository(MongoDbContext context) : ISkillStatsRepository
{
    // Лічильники за активними записами; topLanguages — три найпоширеніші мови (як запит 8 у db/p3/matching.queries.js).
    private static readonly BsonDocument[] Pipeline =
    [
        BsonDocument.Parse("{ $match: { status: 'Active' } }"),
        BsonDocument.Parse(@"{ $group: {
            _id: '$skillId',
            skillName: { $first: '$skillName' },
            offersCount: { $sum: { $cond: [{ $eq: ['$type', 'Offer'] }, 1, 0] } },
            wantsCount: { $sum: { $cond: [{ $eq: ['$type', 'Want'] }, 1, 0] } },
            langs: { $push: '$languages.code' } } }"),
        BsonDocument.Parse(@"{ $set: { langs: { $reduce: {
            input: '$langs', initialValue: [], in: { $concatArrays: ['$$value', '$$this'] } } } } }"),
        BsonDocument.Parse(@"{ $set: { topLanguages: { $slice: [ { $sortArray: {
            input: { $map: { input: { $setUnion: ['$langs', []] }, as: 'c', in: {
                code: '$$c',
                count: { $size: { $filter: { input: '$langs', cond: { $eq: ['$$this', '$$c'] } } } } } } },
            sortBy: { count: -1, code: 1 } } }, 3 ] } } }"),
        BsonDocument.Parse(@"{ $project: { skillName: 1, offersCount: 1, wantsCount: 1, topLanguages: 1,
            updatedAt: '$$NOW' } }"),
        BsonDocument.Parse("{ $merge: { into: 'skillStats', on: '_id', whenMatched: 'replace', whenNotMatched: 'insert' } }")
    ];

    public async Task<int> RebuildAsync(CancellationToken ct = default)
    {
        var index = context.Raw(CollectionNames.UserSkillIndex);
        var stats = context.Raw(CollectionNames.SkillStats);

        // $merge записує результат у skillStats; курсор потрібно вичитати, щоб конвеєр виконався.
        await (await index.AggregateAsync<BsonDocument>(Pipeline, cancellationToken: ct)).ToListAsync(ct);

        // Навички без активних записів більше не потрапляють у статистику.
        var activeSkills = await index.Distinct<int>("skillId", new BsonDocument("status", "Active"), cancellationToken: ct)
            .ToListAsync(ct);
        await stats.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Nin("_id", activeSkills), ct);

        return (int)await stats.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct);
    }
}
