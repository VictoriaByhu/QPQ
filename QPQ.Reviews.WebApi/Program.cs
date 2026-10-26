using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using QPQ.Reviews.Application;
using QPQ.Reviews.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Реєстрація мапінгу MongoDB (має бути до ініціалізації бази даних)
QPQ.Reviews.Infrastructure.Persistence.MongoMappings.Register();

// Реєстрація шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Пряма реєстрація IReviewQueries для читання
builder.Services.AddScoped<QPQ.Reviews.Application.Interfaces.IReviewQueries, QPQ.Reviews.Infrastructure.Repositories.ReviewQueries>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<QPQ.Reviews.Infrastructure.Seeding.IDataSeeder, QPQ.Reviews.Infrastructure.Seeding.ReviewSeeder>();
builder.Services.AddHostedService<QPQ.Reviews.Infrastructure.Seeding.IndexCreationService>();

var app = builder.Build();

app.UseMiddleware<QPQ.Reviews.WebApi.Middleware.ExceptionMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<QPQ.Reviews.Infrastructure.Seeding.IDataSeeder>();
    await seeder.SeedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();