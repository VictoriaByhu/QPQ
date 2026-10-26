using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Matching.Tests.Infrastructure;

namespace Matching.Tests.Api;

/// <summary>Піднімає справжній застосунок (усі шари) на тимчасовому mongod із власною базою.</summary>
public sealed class ApiFactory(MongoFixture mongo) : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = mongo.NewDatabaseName("api");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("MongoDb:ConnectionString", mongo.ConnectionString);
        builder.UseSetting("MongoDb:DatabaseName", DatabaseName);
        builder.UseSetting("Database:Seed", "true");
    }
}
