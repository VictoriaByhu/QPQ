using System.Diagnostics;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (ctx, next) =>
{
    var sw = Stopwatch.StartNew();
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Gateway error {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
        throw;
    }

    var route = ctx.GetReverseProxyFeature()?.Route.Config.RouteId;
    app.Logger.LogInformation("{Method} {Path} from {ClientIp} -> route {Route}: {StatusCode} in {Elapsed} ms",
        ctx.Request.Method, ctx.Request.Path, ctx.Connection.RemoteIpAddress, route,
        ctx.Response.StatusCode, sw.ElapsedMilliseconds);
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
