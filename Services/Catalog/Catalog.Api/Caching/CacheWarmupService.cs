using Catalog.Bll.Dtos;
using Catalog.Bll.Services;

namespace Catalog.Api.Caching;

/// <summary>
/// Прогрівання кешу: під час старту й потім періодично завантажує найзапитуваніші довідкові дані
/// (теги, категорії, першу сторінку навичок і їх картки) — перші запити не чекають базу.
/// Працює через ті самі кешовані сервіси, тож заповнює і L1, і Redis. Помилки лише логуються.
/// Вимикається в appsettings: Cache:Warmup:Enabled = false.
/// </summary>
public sealed class CacheWarmupService(
    IServiceScopeFactory scopes, IConfiguration config, ILogger<CacheWarmupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!config.GetValue("Cache:Warmup:Enabled", true)) return;

        var interval = TimeSpan.FromMinutes(config.GetValue("Cache:Warmup:IntervalMinutes", 5));
        using var timer = new PeriodicTimer(interval);
        do
        {
            await WarmAsync(stoppingToken);
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task WarmAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var tags = scope.ServiceProvider.GetRequiredService<ITagService>();
            var categories = scope.ServiceProvider.GetRequiredService<ICategoryService>();
            var skills = scope.ServiceProvider.GetRequiredService<ISkillService>();

            var tagCount = (await tags.GetAllAsync(ct)).Count;
            var categoryPage = await categories.GetListAsync(new CategoryQuery(), ct);
            var firstPage = await skills.GetListAsync(new SkillQuery(), ct);
            foreach (var item in firstPage.Items)
                await skills.GetByIdAsync(item.Id, ct);

            logger.LogInformation(
                "Cache warm-up finished: {TagCount} tags, {CategoryCount} categories, {SkillCount} skills",
                tagCount, categoryPage.Items.Count, firstPage.Items.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Cache warm-up failed; the cache will fill on demand");
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try { return await timer.WaitForNextTickAsync(ct); }
        catch (OperationCanceledException) { return false; }
    }
}
