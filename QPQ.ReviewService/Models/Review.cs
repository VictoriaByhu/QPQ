using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QPQ.ReviewService.Models
{
    public class Review
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public int SwapRequestId { get; set; }
        public int FromUserId { get; set; }
        public int ToUserId { get; set; }

        public int Rating { get; set; } // від 1 до 5
        public string? Comment { get; set; }

        // Гнучка структура для оцінок (наприклад: "Якість": 5, "Спілкування": 4)
        public Dictionary<string, int>? Criteria { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
