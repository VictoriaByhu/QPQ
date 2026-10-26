using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using QPQ.Reviews.Domain.Entities;
using QPQ.Reviews.Domain.ValueObjects;

namespace QPQ.Reviews.Infrastructure.Persistence;

public static class MongoMappings
{
    public static void Register()
    {
        ConventionRegistry.Register("camelCase",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            },
            _ => true);

        if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
        {
            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.AutoMap();
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Author)))
        {
            BsonClassMap.RegisterClassMap<Author>(cm =>
            {
                cm.MapProperty(a => a.UserId).SetElementName("userId");
                cm.MapProperty(a => a.Username).SetElementName("username");
                cm.MapCreator(a => new Author(a.UserId, a.Username));
            });
        }
        if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
        {
            BsonClassMap.RegisterClassMap<Review>(cm =>
            {
                cm.AutoMap();
                // Мапінг приватного поля колекції
                cm.MapField("_tags").SetElementName("tags");
            });
        }
    }
}