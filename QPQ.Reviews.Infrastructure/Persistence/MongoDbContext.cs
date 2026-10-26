using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Infrastructure.Settings;

namespace QPQ.Reviews.Infrastructure.Persistence;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString ?? "mongodb://localhost:27017");
        _database = client.GetDatabase(settings.Value.DatabaseName);
    }

    public IMongoCollection<Review> Reviews => _database.GetCollection<Review>("reviews");
}