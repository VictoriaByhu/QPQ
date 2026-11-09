using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

// ── Бази даних (контейнери; дані зберігаються між перезапусками завдяки WithDataVolume) ──────────
// Пароль SQL Server — секрет AppHost: dotnet user-secrets set "Parameters:sql-password" "<пароль>"
// (або вводиться в Dashboard під час першого запуску). У репозиторії паролів немає.
var sqlPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", sqlPassword).WithDataVolume();

// Ім'я ресурсу = ім'я рядка підключення в сервісі (ConnectionStrings:<ім'я>), друге значення — ім'я бази на сервері.
var userSkillsDb = sql.AddDatabase("UserSkillsDb", "QPQ_UserSkills");   // Лаб. 02: ADO.NET + Dapper
var catalogDb = sql.AddDatabase("CatalogDb", "QPQ_CatalogEf");           // Лаб. 03: EF Core (міграції самі створюють схему)

var mongo = builder.AddMongoDB("mongo").WithDataVolume();
var matchingDb = mongo.AddDatabase("MatchingDb", "QPQ_Matching");         // Лаб. 04: MongoDB

// UserSkills працює з T-SQL-схемою та збереженими процедурами (db/p1) — застосовуємо їх, щойно база створена.
var scriptsDir = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "db", "p1"));
builder.Eventing.Subscribe<ResourceReadyEvent>(userSkillsDb.Resource, async (e, ct) =>
{
    var logger = e.Services.GetRequiredService<ResourceLoggerService>().GetLogger(userSkillsDb.Resource);
    var connectionString = await userSkillsDb.Resource.ConnectionStringExpression.GetValueAsync(ct)
        ?? throw new InvalidOperationException("UserSkillsDb connection string is not available");

    await SqlScriptRunner.RunAsync(connectionString,
        new[] { "userskills.schema.sql", "userskills.procedures.sql", "userskills.seed.sql" }
            .Select(f => Path.Combine(scriptsDir, f)), logger, ct);
});

// ── Мікросервіси з Лаб. 02–04: лише WithReference(БД) — порти й рядки підключення призначає Aspire ──
var userSkills = builder.AddProject<Projects.UserSkills_Api>("userskills-api")
    .WithReference(userSkillsDb).WaitFor(userSkillsDb)
    .WithHttpHealthCheck("/health");

var catalog = builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb).WaitFor(catalogDb)
    .WithHttpHealthCheck("/health");

var matching = builder.AddProject<Projects.Matching_WebApi>("matching-api")
    .WithReference(matchingDb).WaitFor(matchingDb)
    .WithEnvironment("Database__Seed", "true")                            // демонстраційні дані для статистики пар
    .WithHttpHealthCheck("/health");

// ── Aggregator → Gateway: порядок запуску БД → сервіси → Aggregator → Gateway задається ланцюжками WaitFor ──
var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithHttpEndpoint()                                                   // порт призначає Aspire; знаходиться за іменем "aggregator"
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(userSkills).WithReference(catalog).WithReference(matching)
    .WaitFor(userSkills).WaitFor(catalog).WaitFor(matching)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")                         // єдина точка входу для клієнтів
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithExternalHttpEndpoints()
    .WithReference(userSkills).WithReference(catalog).WithReference(matching)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health", endpointName: "public");

builder.Build().Run();
