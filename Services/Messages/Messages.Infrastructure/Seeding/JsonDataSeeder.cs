using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Messages.Infrastructure.Persistence;

namespace Messages.Infrastructure.Seeding;

public abstract class JsonDataSeeder(MongoDbContext context, ILogger logger) : IDataSeeder
{
    protected abstract string CollectionName { get; }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var documents = await LoadDocumentsAsync(ct);
        var collection = context.Database.GetCollection<BsonDocument>(CollectionName);

        var ids = documents.Select(d => d["_id"]).ToList();
        var existing = await collection
            .Find(Builders<BsonDocument>.Filter.In("_id", ids))
            .Project(Builders<BsonDocument>.Projection.Include("_id"))
            .ToListAsync(ct);
        var existingIds = existing.Select(d => d["_id"]).ToHashSet();

        var missing = documents.Where(d => !existingIds.Contains(d["_id"])).ToList();
        if (missing.Count == 0)
        {
            logger.LogInformation("Seed data for {Collection} is already present ({Count} documents)",
                CollectionName, documents.Count);
            return;
        }

        await collection.InsertManyAsync(missing, new InsertManyOptions { IsOrdered = false }, ct);
        logger.LogInformation("Seeded {Inserted} of {Total} documents into {Collection}",
            missing.Count, documents.Count, CollectionName);
    }

    private async Task<List<BsonDocument>> LoadDocumentsAsync(CancellationToken ct)
    {
        var resourceName = $"{typeof(JsonDataSeeder).Namespace}.Data.{CollectionName}.json";
        await using var stream = typeof(JsonDataSeeder).Assembly.GetManifestResourceStream(resourceName)
                                 ?? throw new InvalidOperationException($"Seed resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(ct);
        return BsonSerializer.Deserialize<BsonArray>(json).Select(v => v.AsBsonDocument).ToList();
    }
}
