using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Matching.Infrastructure.Persistence;

/// <summary>
/// Створює індекси при старті (імена й параметри збігаються з db/p3/matching.collections.js).
/// Повторне створення наявного індексу безпечне: MongoDB його не дублює.
/// </summary>
public sealed class IndexCreationService(MongoDbContext context, ILogger<IndexCreationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            await CreateIndexesAsync(context, ct);
            logger.LogInformation("MongoDB indexes are in place for database {Database}", context.Database.DatabaseNamespace.DatabaseName);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            // Сервіс стартує й без бази: запити тоді повертатимуть 503 (див. GlobalExceptionHandler).
            logger.LogError(ex, "Could not create MongoDB indexes; is MongoDB reachable?");
        }
    }

    public static async Task CreateIndexesAsync(MongoDbContext context, CancellationToken ct = default)
    {
        await context.Raw(CollectionNames.UserSkillIndex).Indexes.CreateManyAsync(new[]
        {
            // один документ на запис UserSkills: оновлення за подією UserSkillsChanged
            new CreateIndexModel<BsonDocument>(new BsonDocument("userSkillId", 1),
                new CreateIndexOptions { Name = "ux_userSkillIndex_userSkillId", Unique = true }),
            // пошук активних пропозицій і бажань за навичкою (частковий індекс)
            new CreateIndexModel<BsonDocument>(new BsonDocument { { "skillId", 1 }, { "type", 1 } },
                new CreateIndexOptions<BsonDocument>
                {
                    Name = "ix_userSkillIndex_skill_type_active",
                    PartialFilterExpression = new BsonDocument("status", "Active")
                }),
            // усі записи користувача (видалення за UserDeleted, перегляд)
            new CreateIndexModel<BsonDocument>(new BsonDocument { { "userId", 1 }, { "status", 1 } },
                new CreateIndexOptions { Name = "ix_userSkillIndex_user_status" })
        }, ct);

        await context.Raw(CollectionNames.Matches).Indexes.CreateManyAsync(new[]
        {
            // одна пара на комбінацію «хто, кому, яку навичку»
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { { "offer.fromUserId", 1 }, { "offer.toUserId", 1 }, { "offer.skillId", 1 } },
                new CreateIndexOptions { Name = "ux_matches_offer", Unique = true }),
            // список пар користувача за оцінкою
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { { "userIds", 1 }, { "status", 1 }, { "score", -1 } },
                new CreateIndexOptions { Name = "ix_matches_user_status_score" }),
            // автоматичне видалення застарілих пар
            new CreateIndexModel<BsonDocument>(new BsonDocument("expiresAt", 1),
                new CreateIndexOptions { Name = "ttl_matches_expiresAt", ExpireAfter = TimeSpan.Zero })
        }, ct);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
