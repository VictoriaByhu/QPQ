using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Domain.Common;
using Matching.Domain.Exceptions;

namespace Matching.Infrastructure.Persistence.Repositories;

/// <summary>Оптимістична конкурентність: заміна документа лише з очікуваною версією.</summary>
internal static class VersionedWrites
{
    public static async Task ReplaceAsync<T>(
        IMongoCollection<T> collection, T entity, string what, CancellationToken ct) where T : BaseEntity
    {
        var expectedVersion = entity.Version;
        entity.BumpVersion();

        // Документи, створені скриптом db/p3 до появи поля version, вважаються версією 0.
        var versionFilter = expectedVersion == 0
            ? Builders<T>.Filter.Or(
                Builders<T>.Filter.Eq(x => x.Version, 0),
                Builders<T>.Filter.Exists(x => x.Version, false))
            : Builders<T>.Filter.Eq(x => x.Version, expectedVersion);

        var result = await collection.ReplaceOneAsync(
            Builders<T>.Filter.And(Builders<T>.Filter.Eq(x => x.Id, entity.Id), versionFilter),
            entity, cancellationToken: ct);

        if (result.MatchedCount == 0)                       // хтось змінив або видалив документ раніше
            throw new ConflictException($"{what} {entity.Id} was modified or deleted by another request");
    }
}
