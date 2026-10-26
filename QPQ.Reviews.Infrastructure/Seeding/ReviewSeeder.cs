using Microsoft.Extensions.Options;
using MongoDB.Driver;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Domain.ValueObjects;
using QPQ.Reviews.Infrastructure.Settings;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Infrastructure.Seeding;

public class ReviewSeeder : IDataSeeder
{
    private readonly IMongoCollection<Review> _reviewsCollection;

    public ReviewSeeder(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
    {
        var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
        _reviewsCollection = database.GetCollection<Review>(settings.Value.ReviewsCollectionName);
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _reviewsCollection.EstimatedDocumentCountAsync(cancellationToken: ct) > 0)
            return;

        var reviews = new[]
        {
            new Review("42", new Author("u-17", "Олена"), 5, "Чудовий екран"),
            new Review("42", new Author("u-21", "Андрій"), 4, "Трохи важкий")
        };

        await _reviewsCollection.InsertManyAsync(reviews, cancellationToken: ct);
    }
}