using QPQ.Moderation.Api.Infrastructure;
using QPQ.Moderation.Bll.Interfaces;
using QPQ.Moderation.Bll.Mapping;
using QPQ.Moderation.Bll.Services;
using QPQ.Moderation.Dal; // Тепер він тягнеться з правильної папки
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Налаштування логування Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Отримання рядка підключення
var connectionString = builder.Configuration.GetConnectionString("ModerationDb")
    ?? throw new InvalidOperationException("Connection string 'ModerationDb' is missing");

// Реєстрація залежностей (DI)
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(); // Включаємо наш глобальний обробник помилок

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.Run();