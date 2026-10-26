using FluentValidation;
using MediatR;
using Matching.Application.Common.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Matching.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            // MediatR 13+: безкоштовна Community-ліцензія; ключ береться з User Secrets / змінних оточення.
            var licenseKey = config["LuckyPenny:LicenseKey"];
            if (!string.IsNullOrWhiteSpace(licenseKey))
                cfg.LicenseKey = licenseKey;

            cfg.RegisterServicesFromAssembly(assembly);

            // Порядок реєстрації = порядок виконання: зовнішній -> внутрішній.
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));       // 1: бачить усе, включно з відхиленими запитами
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));    // 2: відхиляє некоректні дані до обробника
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));   // 3: вимірює лише час обробника
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
