using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace QPQ.ReviewService.Models
{
    public class Discussion
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public int SkillId { get; set; }
        public string? SkillName { get; set; } // Дублювання даних для уникнення JOIN

        public int AuthorUserId { get; set; }
        public string? AuthorUserName { get; set; } // Дублювання даних

        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Вкладені коментарі (Embedded Documents)
        public List<Comment> Comments { get; set; } = new List<Comment>();
    }

    public class Comment
    {
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}