var builder = DistributedApplication.CreateBuilder(args);

// Ім'я ресурсу бази збігається з іменем рядка підключення в API: Aspire передає його автоматично.
var userSkillsDb = builder.AddConnectionString("UserSkillsDb");

builder.AddProject<Projects.UserSkills_Api>("userskills-api")
    .WithReference(userSkillsDb);

// Catalog (Лаб. 03, EF Core): ресурс CatalogDb передає рядок підключення в Catalog.Api.
var catalogDb = builder.AddConnectionString("CatalogDb");

builder.AddProject<Projects.Catalog_Api>("catalog-api")
    .WithReference(catalogDb);

// Matching (Лаб. 04, MongoDB, Clean Architecture): ресурс MatchingDb передає рядок підключення MongoDB.
var matchingDb = builder.AddConnectionString("MatchingDb");

builder.AddProject<Projects.Matching_WebApi>("matching-api")
    .WithReference(matchingDb);

builder.Build().Run();
