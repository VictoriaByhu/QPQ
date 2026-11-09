using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Platform.Caching;

namespace Matching.Application.Caching;

/// <summary>Групи кешу: запити однієї групи інвалідуються разом (піднімається версія групи, що входить у ключ).</summary>
public static class CacheGroups
{
    public const string SkillStats = "skill-stats";
    public const string Matches = "matches";
    public const string IndexEntries = "index-entries";
}

public enum CacheKind { Entity, List }

/// <summary>Позначає запит, відповідь якого кешується (L1 + L2). Ключ = група + версія групи + ім'я запиту + хеш його параметрів.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class CacheableAttribute(string group, CacheKind kind = CacheKind.List) : Attribute
{
    public string Group { get; } = group;
    public CachePolicy Policy { get; } = kind == CacheKind.Entity ? CachePolicy.Entity : CachePolicy.List;
}

/// <summary>Позначає команду, після якої застарівають кеші вказаних груп (інвалідація одразу після успішного запису).</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class InvalidatesCacheAttribute(params string[] groups) : Attribute
{
    public IReadOnlyCollection<string> Groups { get; } = groups;
}

/// <summary>Обгортка, щоб кешувати будь-який тип відповіді (ITwoLevelCache приймає лише reference-типи).</summary>
public sealed record CacheBox<T>(T Value);

/// <summary>
/// MediatR-behavior кешування (cache-aside) для запитів і інвалідації для команд. Найвнутрішніший у конвеєрі:
/// валідація вже відбулась, а на кеш-попаданні не виконується ні обробник, ні запит до MongoDB.
/// Виняток обробника (NotFound тощо) не кешується й іде нагору як є.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>(ITwoLevelCache cache)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    private static readonly CacheableAttribute? Cacheable = typeof(TRequest).GetCustomAttribute<CacheableAttribute>();
    private static readonly InvalidatesCacheAttribute? Invalidates = typeof(TRequest).GetCustomAttribute<InvalidatesCacheAttribute>();

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        if (Cacheable is { } rule)
        {
            var version = await cache.GetVersionAsync(rule.Group, ct);
            var key = $"matching:{rule.Group}:{version}:{typeof(TRequest).Name}:{Hash(request)}";
            var boxed = await cache.GetOrCreateAsync(key, rule.Policy,
                async c => new CacheBox<TResponse>(await next(c)), ct);
            return boxed!.Value;
        }

        var response = await next(ct);
        if (Invalidates is { } inv)
            foreach (var group in inv.Groups)
                await cache.BumpVersionAsync(group, typeof(TRequest).Name, ct);
        return response;
    }

    private static string Hash(TRequest request) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request))))[..16];
}
