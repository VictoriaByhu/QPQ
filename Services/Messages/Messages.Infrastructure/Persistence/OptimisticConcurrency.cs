using MongoDB.Bson;
using MongoDB.Driver;
using Messages.Domain.Common;
using Messages.Domain.Exceptions;

namespace Messages.Infrastructure.Persistence;

public static class OptimisticConcurrency
{
    public static FilterDefinition<T> ByIdAndVersion<T>(ObjectId id, int version) where T : BaseEntity
    {
        var filter = Builders<T>.Filter;
        var versionFilter = version == 0
            ? filter.Or(filter.Eq(e => e.Version, 0), filter.Exists(e => e.Version, false))
            : filter.Eq(e => e.Version, version);
        return filter.Eq(e => e.Id, id) & versionFilter;
    }

    public static async Task ReplaceWithVersionAsync<T>(
        this IMongoCollection<T> collection, T entity, CancellationToken ct) where T : BaseEntity
    {
        var expectedVersion = entity.Version;
        entity.BumpVersion();

        var result = await collection.ReplaceOneAsync(
            ByIdAndVersion<T>(entity.Id, expectedVersion), entity, cancellationToken: ct);

        if (result.MatchedCount == 0)
            throw new ConflictException(
                $"{typeof(T).Name} {entity.Id} was modified or deleted by another request (expected version {expectedVersion}).");
    }

    public static async Task DeleteWithVersionAsync<T>(
        this IMongoCollection<T> collection, T entity, CancellationToken ct) where T : BaseEntity
    {
        var result = await collection.DeleteOneAsync(ByIdAndVersion<T>(entity.Id, entity.Version), ct);
        if (result.DeletedCount == 0)
            throw new ConflictException(
                $"{typeof(T).Name} {entity.Id} was modified or deleted by another request (expected version {entity.Version}).");
    }
}
