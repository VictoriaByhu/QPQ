using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Infrastructure.Persistence;
using QPQ.Reviews.Infrastructure.Repositories;
using QPQ.Reviews.Infrastructure.Settings;

namespace QPQ.Reviews.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection("MongoDbSettings"));

        services.AddSingleton<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();

        return services;
    }
}