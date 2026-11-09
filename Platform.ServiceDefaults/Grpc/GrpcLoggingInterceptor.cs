using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Grpc;

/// <summary>
/// Серверний interceptor для унарних gRPC-викликів: структуроване логування методу, адреси клієнта (peer),
/// статусу й тривалості. Логи всередині виклику (зокрема cache-логи) отримують властивість Protocol=gRPC,
/// тому попадання в кеш для gRPC відрізняються від HTTP. Статус додається й до поточного span.
/// </summary>
public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        using var scope = logger.BeginScope(new Dictionary<string, object> { ["Protocol"] = "gRPC" });
        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} from {Peer}: OK in {Elapsed} ms",
                context.Method, context.Peer, sw.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            // NotFound / InvalidArgument — штатні відповіді, а не збої сервера, тому Warning.
            logger.LogWarning(ex, "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms",
                context.Method, context.Peer, ex.StatusCode, sw.ElapsedMilliseconds);
            Activity.Current?.SetTag("rpc.grpc.status_code", (int)ex.StatusCode);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "gRPC {Method} from {Peer}: unhandled error in {Elapsed} ms",
                context.Method, context.Peer, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
