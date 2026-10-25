using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Messages.Infrastructure.Persistence;

namespace Messages.Infrastructure.Seeding;

public sealed class DataSeedingService(
    IEnumerable<IDataSeeder> seeders,
    IOptions<MongoDbOptions> options,
    ILogger<DataSeedingService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!options.Value.SeedData)
        {
            logger.LogInformation("MongoDB seeding is disabled (MongoDb:SeedData = false)");
            return;
        }

        try
        {
            foreach (var seeder in seeders)
                await seeder.SeedAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is MongoException or TimeoutException)
        {
            logger.LogError(ex, "Failed to seed MongoDB");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
