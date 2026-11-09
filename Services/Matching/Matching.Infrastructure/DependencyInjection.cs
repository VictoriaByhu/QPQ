using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Extensions.DiagnosticSources;
using Matching.Application.Common.Interfaces;
using Matching.Domain.Interfaces;
using Matching.Infrastructure.Persistence;
using Matching.Infrastructure.Persistence.Queries;
using Matching.Infrastructure.Persistence.Repositories;
using Matching.Infrastructure.Seeding;

namespace Matching.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        MongoMappings.Register();                                 // до першого звернення до БД

        services.AddOptions<MongoDbOptions>()
            .Bind(config.GetSection(MongoDbOptions.SectionName))
            .PostConfigure(o =>
            {
                // Aspire (ресурс MatchingDb) передає рядок підключення як ConnectionStrings:MatchingDb.
                var aspire = config.GetConnectionString("MatchingDb");
                if (!string.IsNullOrWhiteSpace(aspire)) o.ConnectionString = aspire;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Клієнт MongoDB потокобезпечний і створюється один раз; контекст отримує його через DI.
        // Підписка на DiagnosticSources: кожна команда MongoDB стає span у розподіленому трасуванні (Лаб. 05).
        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = MongoClientSettings.FromConnectionString(
                sp.GetRequiredService<IOptions<MongoDbOptions>>().Value.ConnectionString);
            settings.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
            return new MongoClient(settings);
        });
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IUserSkillIndexRepository, UserSkillIndexRepository>();
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<ISkillStatsRepository, SkillStatsRepository>();

        services.AddScoped<IUserSkillIndexQueries, UserSkillIndexQueries>();
        services.AddScoped<IMatchQueries, MatchQueries>();
        services.AddScoped<ISkillStatsQueries, SkillStatsQueries>();

        services.AddHostedService<IndexCreationService>();       // індекси — першими

        if (config.GetValue("Database:Seed", false))
        {
            services.AddScoped<IDataSeeder, UserSkillIndexSeeder>();
            services.AddScoped<IDataSeeder, MatchSeeder>();
            services.AddScoped<IDataSeeder, SkillStatsSeeder>();
            services.AddHostedService<DataSeedingHostedService>();
        }

        return services;
    }
}
