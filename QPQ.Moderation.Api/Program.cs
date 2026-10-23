using Microsoft.EntityFrameworkCore;
using QPQ.Moderation.Api.Infrastructure;
using QPQ.Moderation.Bll.Interfaces;
using QPQ.Moderation.Bll.Mapping;
using QPQ.Moderation.Bll.Services;
using QPQ.Moderation.Dal;
using Serilog;
using Serilog.Events;
using FluentValidation;
using QPQ.Moderation.Bll.Validators;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Реєстрація валідаторів FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateReportDtoValidator>();

// Налаштування логування Serilog (з SQL-запитами)
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Отримання рядка підключення
var connectionString = builder.Configuration.GetConnectionString("ModerationDb")
    ?? throw new InvalidOperationException("Connection string 'ModerationDb' is missing");

// ПІДКЛЮЧЕННЯ EF CORE
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Реєстрація репозиторіїв та сервісів
builder.Services.AddScoped<QPQ.Moderation.Dal.Interfaces.IUnitOfWork, QPQ.Moderation.Dal.UnitOfWork.UnitOfWork>();
builder.Services.AddScoped<IReportService, ReportService>();

// Реєстрація AutoMapper
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<ModerationProfile>();
});

// Налаштування обробки помилок ProblemDetails
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(); // Глобальний обробник помилок
app.UseSerilogRequestLogging(); // Логування HTTP-запитів

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Міграції та сідінг
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await QPQ.Moderation.Dal.DbInitializer.SeedAsync(db);
    }
}

app.Run();