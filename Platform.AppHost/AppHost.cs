var builder = DistributedApplication.CreateBuilder(args);

// Ім'я ресурсу бази збігається з іменем рядка підключення в API: Aspire передає його автоматично.
var userSkillsDb = builder.AddConnectionString("UserSkillsDb");

builder.AddProject<Projects.UserSkills_Api>("userskills-api")
    .WithReference(userSkillsDb);

builder.Build().Run();
