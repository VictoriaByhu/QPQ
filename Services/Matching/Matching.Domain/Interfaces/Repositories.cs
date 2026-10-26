using MongoDB.Bson;
using Matching.Domain.Entities;
using Matching.Domain.Enums;

namespace Matching.Domain.Interfaces;

/// <summary>Репозиторій записів індексу підбору. Реалізація — в Infrastructure (MongoDB).</summary>
public interface IUserSkillIndexRepository
{
    Task<UserSkillIndexEntry?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task<bool> ExistsByUserSkillIdAsync(int userSkillId, CancellationToken ct = default);

    /// <summary>Активні записи за критеріями; фільтрація виконується в БД, а не в пам'яті.</summary>
    Task<IReadOnlyList<UserSkillIndexEntry>> FindActiveAsync(
        string? userId, int? skillId, UserSkillType? type, string? excludeUserId,
        CancellationToken ct = default);

    Task AddAsync(UserSkillIndexEntry entry, CancellationToken ct = default);

    /// <summary>Оновлює документ, лише якщо його версія в БД дорівнює очікуваній; інакше ConflictException.</summary>
    Task UpdateAsync(UserSkillIndexEntry entry, CancellationToken ct = default);

    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default);
}

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(ObjectId id, CancellationToken ct = default);

    /// <summary>Пара за ключем унікального індексу: «хто, кому, яку навичку».</summary>
    Task<Match?> GetByOfferKeyAsync(string fromUserId, string toUserId, int skillId, CancellationToken ct = default);

    Task AddAsync(Match match, CancellationToken ct = default);

    /// <summary>Оновлення з перевіркою версії (ConflictException при розбіжності).</summary>
    Task UpdateAsync(Match match, CancellationToken ct = default);
}

public interface ISkillStatsRepository
{
    /// <summary>Перераховує skillStats агрегацією з $merge. Повертає кількість документів у колекції.</summary>
    Task<int> RebuildAsync(CancellationToken ct = default);
}
