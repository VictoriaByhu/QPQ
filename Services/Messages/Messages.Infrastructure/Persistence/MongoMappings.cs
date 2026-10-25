using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Messages.Domain.Common;
using Messages.Domain.Constants;
using Messages.Domain.Entities;
using Messages.Domain.ValueObjects;

namespace Messages.Infrastructure.Persistence;

public static class MongoMappings
{
    public const string DiscriminatorElement = "type";

    private const string DefaultConventionsName = "__defaults__";

    private static int _registered;

    private static bool IsEntity(Type type) => typeof(BaseEntity).IsAssignableFrom(type);

    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
            return;

        ConventionRegistry.Remove(DefaultConventionsName);
        ConventionRegistry.Register(DefaultConventionsName, DefaultConventionPack.Instance, t => !IsEntity(t));
        var entityDefaults = new ConventionPack();
        entityDefaults.AddRange(DefaultConventionPack.Instance.Conventions
            .Where(c => c is not ImmutableTypeClassMapConvention));
        ConventionRegistry.Register("messages-entity-defaults", entityDefaults, IsEntity);

        ConventionRegistry.Register("messages-conventions",
            new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new EnumRepresentationConvention(BsonType.String)
            },
            _ => true);

        BsonSerializer.RegisterDiscriminatorConvention(
            typeof(Message), new ScalarDiscriminatorConvention(DiscriminatorElement));

        RegisterValueObjects();
        RegisterEntities();
    }

    private static void RegisterValueObjects()
    {
        BsonClassMap.RegisterClassMap<Participant>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(p => new Participant(p.UserId, p.Role, p.LastReadAt));
        });

        BsonClassMap.RegisterClassMap<LastMessage>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(m => new LastMessage(m.MessageId, m.SenderId, m.Preview, m.SentAt));
        });

        BsonClassMap.RegisterClassMap<AttachmentRef>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(a => new AttachmentRef(a.AttachmentId, a.FileName));
        });

        BsonClassMap.RegisterClassMap<SystemEventDetails>(cm =>
        {
            cm.AutoMap();
            cm.MapCreator(d => new SystemEventDetails(d.SwapId));
        });
    }

    private static void RegisterEntities()
    {
        BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
        {
            cm.AutoMap();
            cm.MapIdMember(e => e.Id);
            cm.GetMemberMap(e => e.CreatedAt).SetShouldSerializeMethod(e => e is not Attachment);
            cm.GetMemberMap(e => e.UpdatedAt).SetShouldSerializeMethod(e => e is Conversation);
            cm.GetMemberMap(e => e.Version).SetShouldSerializeMethod(e => e is not Attachment);
        });

        BsonClassMap.RegisterClassMap<Conversation>(cm =>
        {
            cm.AutoMap();
            cm.UnmapProperty(c => c.Participants);
            cm.MapField("_participants").SetElementName("participants");
        });

        BsonClassMap.RegisterClassMap<Message>(cm =>
        {
            cm.AutoMap();
            cm.AddKnownType(typeof(TextMessage));
            cm.AddKnownType(typeof(FileMessage));
            cm.AddKnownType(typeof(SystemMessage));
        });

        BsonClassMap.RegisterClassMap<TextMessage>(cm =>
        {
            cm.AutoMap();
            cm.SetDiscriminator(MessageTypes.Text);
        });

        BsonClassMap.RegisterClassMap<FileMessage>(cm =>
        {
            cm.AutoMap();
            cm.SetDiscriminator(MessageTypes.File);
            cm.UnmapProperty(m => m.Attachments);
            cm.MapField("_attachments").SetElementName("attachments");
            cm.GetMemberMap(m => m.Caption).SetIgnoreIfNull(true);
        });

        BsonClassMap.RegisterClassMap<SystemMessage>(cm =>
        {
            cm.AutoMap();
            cm.SetDiscriminator(MessageTypes.System);
        });

        BsonClassMap.RegisterClassMap<Attachment>(cm =>
        {
            cm.AutoMap();
        });
    }
}
