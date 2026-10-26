using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Matching.Infrastructure.Persistence;

namespace Matching.Tests.Infrastructure;

/// <summary>
/// Де взяти MongoDB для інтеграційних тестів (за пріоритетом): змінна оточення QPQ_TEST_MONGO,
/// User Secrets сервісу (MongoDb:ConnectionString, той самий, що використовує Matching.WebApi у Development),
/// інакше mongodb://localhost:27017. Якщо сервер недоступний, тести з [MongoFact] пропускаються з поясненням.
/// Кожен тест створює власну базу й видаляє її наприкінці, тому спільний сервер безпечний.
/// </summary>
public static class MongoAvailability
{
    private const string WebApiUserSecretsId = "5e8f1b3a-7c2d-4a96-b0e4-9d1c6a3f8b27";

    public static string Uri { get; } = Resolve();

    private static string Resolve()
    {
        if (Environment.GetEnvironmentVariable("QPQ_TEST_MONGO") is { Length: > 0 } fromEnv) return fromEnv;

        var secrets = new ConfigurationBuilder().AddUserSecrets(WebApiUserSecretsId).Build();
        return secrets["MongoDb:ConnectionString"] is { Length: > 0 } fromSecrets
            ? fromSecrets
            : "mongodb://localhost:27017";
    }

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
            Skip = $"MongoDB is not reachable. Start it (see Services/Matching/README.md) or set QPQ_TEST_MONGO.";
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
