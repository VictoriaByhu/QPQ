using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Matching.Infrastructure.Seeding;

/// <summary>Виконує всі <see cref="IDataSeeder"/> при старті (після створення індексів).</summary>
public sealed class DataSeedingHostedService(IServiceScopeFactory scopes, ILogger<DataSeedingHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            foreach (var seeder in scope.ServiceProvider.GetServices<IDataSeeder>())
                await seeder.SeedAsync(ct);
            logger.LogInformation("MongoDB seeding finished");
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            logger.LogError(ex, "Seeding skipped: MongoDB is not reachable");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
