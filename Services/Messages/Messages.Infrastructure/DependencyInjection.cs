using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Messages.Application.Common.Interfaces;
using Messages.Domain.Interfaces;
using Messages.Infrastructure.HealthChecks;
using Messages.Infrastructure.Persistence;
using Messages.Infrastructure.Persistence.Queries;
using Messages.Infrastructure.Persistence.Repositories;
using Messages.Infrastructure.Seeding;

namespace Messages.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        MongoMappings.Register();

        services.AddOptions<MongoDbOptions>()
            .Bind(configuration.GetSection(MongoDbOptions.SectionName))
            .PostConfigure(options =>
            {
                var connectionString = configuration.GetConnectionString(MongoDbOptions.ConnectionStringName);
                if (!string.IsNullOrWhiteSpace(connectionString))
                    options.ConnectionString = connectionString;
            })
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString) && !string.IsNullOrWhiteSpace(o.DatabaseName),
                "MongoDb:ConnectionString and MongoDb:DatabaseName must be configured.")
            .ValidateOnStart();

        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<IMessageQueries, MessageQueries>();

        services.AddSingleton<IDataSeeder, ConversationSeeder>();
        services.AddSingleton<IDataSeeder, AttachmentSeeder>();
        services.AddSingleton<IDataSeeder, MessageSeeder>();

        services.AddHostedService<IndexCreationService>();
        services.AddHostedService<DataSeedingService>();

        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb", tags: ["db", "mongodb"]);

        return services;
    }
}
