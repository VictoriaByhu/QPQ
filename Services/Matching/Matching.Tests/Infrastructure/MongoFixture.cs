using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Matching.Infrastructure.Persistence;

namespace Matching.Tests.Infrastructure;

/// <summary>
/// Де взяти MongoDB для інтеграційних тестів: змінна оточення QPQ_TEST_MONGO
/// (наприклад, mongodb://localhost:27018) або mongodb://localhost:27017 за замовчуванням.
/// Якщо сервер недоступний, тести з [MongoFact] пропускаються з поясненням.
/// </summary>
public static class MongoAvailability
{
    public static string Uri { get; } =
        Environment.GetEnvironmentVariable("QPQ_TEST_MONGO") is { Length: > 0 } uri ? uri : "mongodb://localhost:27017";

    public static bool IsReachable { get; } = Ping();

    private static bool Ping()
    {
        try
        {
            var settings = MongoClientSettings.FromConnectionString(Uri);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
            new MongoClient(settings).GetDatabase("admin").RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>[Fact], який виконується лише коли доступний MongoDB.</summary>
public sealed class MongoFactAttribute : FactAttribute
{
    public MongoFactAttribute()
    {
        if (!MongoAvailability.IsReachable)
            Skip = $"MongoDB is not reachable at {MongoAvailability.Uri}. Start it (see Services/Matching/README.md) or set QPQ_TEST_MONGO.";
    }
}

/// <summary>Один сервер на всі інтеграційні тести; кожен тест працює у власній базі, яка видаляється наприкінці.</summary>
public sealed class MongoFixture : IDisposable
{
    private readonly List<string> _databases = [];

    public MongoFixture() => MongoMappings.Register();

    public string ConnectionString => MongoAvailability.Uri;

    public MongoDbContext NewContext() => new(Options.Create(new MongoDbOptions
    {
        ConnectionString = ConnectionString,
        DatabaseName = NewDatabaseName("t")
    }));

    public string NewDatabaseName(string prefix)
    {
        var name = prefix + "_" + Guid.NewGuid().ToString("N");
        lock (_databases) _databases.Add(name);
        return name;
    }

    public void Dispose()
    {
        if (!MongoAvailability.IsReachable) return;
        var client = new MongoClient(ConnectionString);
        foreach (var db in _databases) client.DropDatabase(db);
    }
}

[CollectionDefinition(Name)]
public sealed class MongoCollection : ICollectionFixture<MongoFixture>
{
    public const string Name = "mongo";
}
