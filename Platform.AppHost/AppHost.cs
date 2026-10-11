var builder = DistributedApplication.CreateBuilder(args);

// Ім'я ресурсу бази збігається з іменем рядка підключення в API: Aspire передає його автоматично.
var swapsDb = builder.AddConnectionString("SwapsDb");
var userSkillsDb = builder.AddConnectionString("UserSkillsDb");

builder.AddProject<Projects.Swaps_Api>("swaps-api")
    .WithReference(swapsDb);

builder.AddProject<Projects.UserSkills_Api>("userskills-api")
    .WithReference(userSkillsDb);

builder.Build().Run();
