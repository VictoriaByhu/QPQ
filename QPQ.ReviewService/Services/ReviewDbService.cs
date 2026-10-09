using MongoDB.Driver;
using QPQ.ReviewService.Models;

namespace QPQ.ReviewService.Services
{
    public class ReviewDbService
    {
        private readonly IMongoCollection<Review> _reviews;
        private readonly IMongoCollection<Discussion> _discussions;

        public ReviewDbService(IConfiguration config)
        {
            var mongoClient = new MongoClient(config["MongoDbSettings:ConnectionString"]);
            var mongoDatabase = mongoClient.GetDatabase(config["MongoDbSettings:DatabaseName"]);

            _reviews = mongoDatabase.GetCollection<Review>("Reviews");
            _discussions = mongoDatabase.GetCollection<Discussion>("Discussions");
        }

        // --- Операції для Відгуків (Reviews) ---
        public async Task<List<Review>> GetReviewsAsync() =>
            await _reviews.Find(_ => true).ToListAsync();

        public async Task<Review?> GetReviewByIdAsync(string id) =>
            await _reviews.Find(x => x.Id == id).FirstOrDefaultAsync();

        public async Task CreateReviewAsync(Review newReview) =>
            await _reviews.InsertOneAsync(newReview);

        // --- Операції для Обговорень (Discussions) ---
        public async Task<List<Discussion>> GetDiscussionsAsync() =>
            await _discussions.Find(_ => true).ToListAsync();

        public async Task CreateDiscussionAsync(Discussion newDiscussion) =>
            await _discussions.InsertOneAsync(newDiscussion);
    }
}