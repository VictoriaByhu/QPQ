using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Infrastructure.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Infrastructure.Seeding;

public class IndexCreationService : IHostedService
{
    private readonly IMongoCollection<Review> _reviewsCollection;

    public IndexCreationService(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _reviewsCollection = database.GetCollection<Review>(settings.Value.ReviewsCollectionName);
    }

    public async Task StartAsync(CancellationToken ct)
    {
        var keys = Builders<Review>.IndexKeys;

        await _reviewsCollection.Indexes.CreateManyAsync(new[]
        {
            new CreateIndexModel<Review>(keys.Ascending(r => r.ProductId).Descending(r => r.CreatedAt)),
            new CreateIndexModel<Review>(keys.Text(r => r.Comment)),
            new CreateIndexModel<Review>(
                keys.Ascending(r => r.ProductId).Ascending(r => r.Author.UserId),
                new CreateIndexOptions { Unique = true })
        }, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}