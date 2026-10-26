using MongoDB.Bson;

namespace Matching.Domain.Common;

/// <summary>
/// Базова сутність, адаптована під MongoDB: ObjectId, службові дати й Version
/// для оптимістичної конкурентності (оновлення застосовується лише до очікуваної версії).
/// </summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; } = ObjectId.GenerateNewId();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; }

    /// <summary>Для десеріалізації.</summary>
    protected BaseEntity() { }

    /// <summary>Для сідінгу й тестів: дозволяє задати детермінований Id та час створення.</summary>
    protected BaseEntity(ObjectId? id, DateTime? createdAt)
    {
        if (id is { } objectId) Id = objectId;
        if (createdAt is { } created) CreatedAt = created;
    }

    protected void Touch() => UpdatedAt = DateTime.UtcNow;

    /// <summary>Збільшує версію; репозиторій викликає це безпосередньо перед збереженням.</summary>
    public void BumpVersion() => Version++;
}
