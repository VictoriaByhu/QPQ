using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Domain.Entities;

namespace Matching.Infrastructure.Persistence;

public static class CollectionNames
{
    public const string UserSkillIndex = "userSkillIndex";
    public const string Matches = "matches";
    public const string SkillStats = "skillStats";
}

/// <summary>Єдина точка доступу до бази QPQ_Matching. Клієнт MongoDB потокобезпечний, тому контекст — singleton.</summary>
public class MongoDbContext
{
    public MongoDbContext(IOptions<MongoDbOptions> options)
    {
        Client = new MongoClient(options.Value.ConnectionString);
        Database = Client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoClient Client { get; }
    public IMongoDatabase Database { get; }

    public IMongoCollection<UserSkillIndexEntry> UserSkillIndex
        => Database.GetCollection<UserSkillIndexEntry>(CollectionNames.UserSkillIndex);

    public IMongoCollection<Match> Matches
        => Database.GetCollection<Match>(CollectionNames.Matches);

    public IMongoCollection<SkillStats> SkillStats
        => Database.GetCollection<SkillStats>(CollectionNames.SkillStats);

    /// <summary>Нетипізований доступ для запитів із проєкцією (читання лише потрібних полів).</summary>
    public IMongoCollection<BsonDocument> Raw(string collectionName)
        => Database.GetCollection<BsonDocument>(collectionName);
}
