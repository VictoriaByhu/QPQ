using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Platform.ServiceDefaults.Grpc;

public static class GrpcExtensions
{
    /// <summary>Змінна оточення, у якій AppHost передає порт gRPC-endpoint'а сервісу.</summary>
    public const string GrpcPortVariable = "GRPC_PORT";

    /// <summary>
    /// gRPC-сервер для мікросервісу: AddGrpc з LoggingInterceptor і окремий Kestrel-endpoint лише з HTTP/2.
    /// gRPC працює поверх HTTP/2, а без TLS (локальний Aspire) протокол не узгоджується через ALPN — тому
    /// REST (HTTP/1.1) лишається на звичайному порту, а gRPC слухає порт із GRPC_PORT у режимі Http2.
    /// Без GRPC_PORT (запуск поза AppHost, тести) конфігурація Kestrel не змінюється.
    /// </summary>
    public static WebApplicationBuilder AddGrpcServerDefaults(this WebApplicationBuilder builder)
    {
        builder.Services.AddGrpc(o =>
        {
            o.Interceptors.Add<GrpcLoggingInterceptor>();
            o.EnableDetailedErrors = builder.Environment.IsDevelopment();
        });

        if (!int.TryParse(builder.Configuration[GrpcPortVariable], out var grpcPort))
            return builder;

        // Якщо Kestrel налаштовується в коді, ASPNETCORE_URLS ігнорується — тому REST-адреси відтворюємо самі.
        var urls = (builder.Configuration["ASPNETCORE_URLS"] ?? builder.Configuration["urls"] ?? "http://localhost:5000")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            foreach (var url in urls)
            {
                var uri = new Uri(url.Replace("://+", "://0.0.0.0").Replace("://*", "://0.0.0.0"));
                var protocols = uri.Scheme == Uri.UriSchemeHttps ? HttpProtocols.Http1AndHttp2 : HttpProtocols.Http1;

                if (uri.Host == "localhost") kestrel.ListenLocalhost(uri.Port, o => o.Protocols = protocols);
                else kestrel.Listen(IPAddress.Parse(uri.Host), uri.Port, o => o.Protocols = protocols);
            }

            kestrel.ListenAnyIP(grpcPort, o => o.Protocols = HttpProtocols.Http2);
        });

        return builder;
    }
}
