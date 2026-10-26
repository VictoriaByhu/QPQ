using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using Matching.Domain.Common;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.ValueObjects;

namespace Matching.Infrastructure.Persistence;

/// <summary>
/// BSON-мапінг доменних класів без атрибутів у Domain (BsonClassMap). Імена полів — camelCase,
/// як у документах Лаб. 01; приватні списки (_languages, _userIds ...) мапляться як поля,
/// незмінні Value Objects створюються через конструктор (MapCreator), тож інваріанти діють і при читанні.
/// Реєстрація виконується один раз при старті, до першого звернення до БД.
/// </summary>
public static class MongoMappings
{
    private static readonly object Gate = new();
    private static bool _registered;

    private static readonly DateTimeSerializer Utc = new(DateTimeKind.Utc);
    private static readonly NullableSerializer<DateTime> UtcNullable = new(Utc);

    public static void Register()
    {
        lock (Gate)
        {
            if (_registered) return;

            // Конвенція: усі доменні типи записуються в camelCase (як у документах Лаб. 01);
            // явні SetElementName нижче лише фіксують імена приватних полів.
            ConventionRegistry.Register("MatchingCamelCase",
                new ConventionPack { new CamelCaseElementNameConvention() },
                t => t.Namespace is not null && t.Namespace.StartsWith("Matching.Domain", StringComparison.Ordinal));

            BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
            {
                cm.MapIdProperty(x => x.Id);
                cm.MapProperty(x => x.CreatedAt).SetElementName("createdAt").SetSerializer(Utc);
                cm.MapProperty(x => x.UpdatedAt).SetElementName("updatedAt").SetSerializer(UtcNullable).SetIgnoreIfNull(true);
                cm.MapProperty(x => x.Version).SetElementName("version");
            });

            BsonClassMap.RegisterClassMap<Language>(cm =>
            {
                cm.MapProperty(x => x.Code).SetElementName("code");
                cm.MapProperty(x => x.Proficiency).SetElementName("proficiency");
                cm.MapCreator(x => new Language(x.Code, x.Proficiency));        // незмінний VO
            });

            BsonClassMap.RegisterClassMap<AvailabilitySlot>(cm =>
            {
                cm.MapProperty(x => x.DayOfWeek).SetElementName("dayOfWeek");
                cm.MapProperty(x => x.Start).SetElementName("start");
                cm.MapProperty(x => x.End).SetElementName("end");
                cm.MapCreator(x => new AvailabilitySlot(x.DayOfWeek, x.Start, x.End));
            });

            BsonClassMap.RegisterClassMap<MatchOffer>(cm =>
            {
                cm.MapProperty(x => x.FromUserId).SetElementName("fromUserId");
                cm.MapProperty(x => x.ToUserId).SetElementName("toUserId");
                cm.MapProperty(x => x.SkillId).SetElementName("skillId");
                cm.MapProperty(x => x.SkillName).SetElementName("skillName");
                cm.MapProperty(x => x.IndexRef).SetElementName("indexRef");
                cm.MapCreator(x => new MatchOffer(x.FromUserId, x.ToUserId, x.SkillId, x.SkillName, x.IndexRef));
            });

            BsonClassMap.RegisterClassMap<LanguageCount>(cm =>
            {
                cm.MapProperty(x => x.Code).SetElementName("code");
                cm.MapProperty(x => x.Count).SetElementName("count");
                cm.MapCreator(x => new LanguageCount(x.Code, x.Count));
            });

            BsonClassMap.RegisterClassMap<UserSkillIndexEntry>(cm =>
            {
                cm.SetIgnoreExtraElements(true);                                                                    // сумісність з новими полями
                cm.MapProperty(x => x.UserSkillId).SetElementName("userSkillId");
                cm.MapProperty(x => x.UserId).SetElementName("userId");
                cm.MapProperty(x => x.SkillId).SetElementName("skillId");
                cm.MapProperty(x => x.SkillName).SetElementName("skillName");
                cm.MapProperty(x => x.Type).SetElementName("type").SetSerializer(new EnumSerializer<UserSkillType>(BsonType.String));
                cm.MapProperty(x => x.Level).SetElementName("level");
                cm.MapProperty(x => x.Status).SetElementName("status").SetSerializer(new EnumSerializer<UserSkillStatus>(BsonType.String));
                cm.MapProperty(x => x.ExperienceYears).SetElementName("experienceYears").SetIgnoreIfNull(true);   // лише в Offer
                cm.MapField("_languages").SetElementName("languages");                                              // приватне поле
                cm.MapField("_availability").SetElementName("availability");
            });

            BsonClassMap.RegisterClassMap<Match>(cm =>
            {
                cm.SetIgnoreExtraElements(true);
                cm.MapProperty(x => x.Kind).SetElementName("kind").SetSerializer(new MatchKindSerializer());
                cm.MapProperty(x => x.Status).SetElementName("status").SetSerializer(new EnumSerializer<MatchStatus>(BsonType.String));
                cm.MapProperty(x => x.Score).SetElementName("score");
                cm.MapField("_userIds").SetElementName("userIds");
                cm.MapProperty(x => x.Offer).SetElementName("offer");
                cm.MapProperty(x => x.CounterOffer).SetElementName("counterOffer").SetIgnoreIfNull(true);          // лише в mutual
                cm.MapField("_commonLanguages").SetElementName("commonLanguages");
                cm.MapProperty(x => x.ExpiresAt).SetElementName("expiresAt").SetSerializer(Utc);
            });

            BsonClassMap.RegisterClassMap<SkillStats>(cm =>
            {
                cm.SetIgnoreExtraElements(true);
                cm.MapIdProperty(x => x.SkillId);                                                                   // _id = skillId
                cm.MapProperty(x => x.SkillName).SetElementName("skillName");
                cm.MapProperty(x => x.OffersCount).SetElementName("offersCount");
                cm.MapProperty(x => x.WantsCount).SetElementName("wantsCount");
                cm.MapField("_topLanguages").SetElementName("topLanguages");
                cm.MapProperty(x => x.UpdatedAt).SetElementName("updatedAt").SetSerializer(Utc);
            });

            _registered = true;
        }
    }
}
