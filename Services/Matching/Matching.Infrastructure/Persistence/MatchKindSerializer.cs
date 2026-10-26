using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Matching.Domain.Enums;

namespace Matching.Infrastructure.Persistence;

/// <summary>
/// Конвертер enum -> рядок у camelCase: OneWay зберігається як "oneWay", Mutual як "mutual"
/// (формат документів matches з db/p3/matching.collections.js).
/// </summary>
public sealed class MatchKindSerializer : SerializerBase<MatchKind>
{
    public override MatchKind Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        => context.Reader.ReadString() switch
        {
            "oneWay" => MatchKind.OneWay,
            "mutual" => MatchKind.Mutual,
            var other => throw new FormatException($"Unknown match kind '{other}'")
        };

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, MatchKind value)
        => context.Writer.WriteString(value switch
        {
            MatchKind.OneWay => "oneWay",
            MatchKind.Mutual => "mutual",
            _ => throw new FormatException($"Unknown match kind '{value}'")
        });
}
