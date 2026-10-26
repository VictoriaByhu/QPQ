using MongoDB.Driver;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Infrastructure.Persistence;
using System.Threading.Tasks;

namespace QPQ.Reviews.Infrastructure.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly MongoDbContext _context;

    public ReviewRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(string id)
    {
        var objectId = MongoDB.Bson.ObjectId.Parse(id);
        return await _context.Reviews.Find(x => x.Id == objectId).FirstOrDefaultAsync();
    }

    public async Task DeleteAsync(string id)
    {
        var objectId = MongoDB.Bson.ObjectId.Parse(id);
        await _context.Reviews.DeleteOneAsync(x => x.Id == objectId);
    }

    public async Task AddAsync(Review review)
    {
        await _context.Reviews.InsertOneAsync(review);
    }

    public async Task UpdateAsync(Review review)
    {
        // Оскільки ми викликали BumpVersion() в домені, поточна версія в базі на 1 менша
        var expectedVersion = review.Version - 1;

        var result = await _context.Reviews.ReplaceOneAsync(
            x => x.Id == review.Id && x.Version == expectedVersion,
            review);

        if (result.MatchedCount == 0)
        {
            throw new QPQ.Reviews.Domain.Exceptions.ConflictException($"Review {review.Id} was modified by another user.");
        }
    }
}