using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Platform.ServiceDefaults.Grpc;

public sealed class GrpcLoggingInterceptor(ILogger<GrpcLoggingInterceptor> logger) : Interceptor
{
    private const string LogTemplate = "gRPC {Method} from {Peer}: {Status} in {Elapsed} ms";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var activity = Activity.Current;
        TagRpc(activity, context.Method);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await continuation(request, context);
            logger.LogInformation(LogTemplate, context.Method, context.Peer, StatusCode.OK, stopwatch.ElapsedMilliseconds);
            activity?.SetTag("rpc.grpc.status_code", (int)StatusCode.OK);
            return response;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(LogTemplate, context.Method, context.Peer, ex.StatusCode, stopwatch.ElapsedMilliseconds);
            activity?.SetTag("rpc.grpc.status_code", (int)ex.StatusCode);
            throw;
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            logger.LogInformation(LogTemplate, context.Method, context.Peer, StatusCode.Cancelled, stopwatch.ElapsedMilliseconds);
            activity?.SetTag("rpc.grpc.status_code", (int)StatusCode.Cancelled);
            throw new RpcException(new Status(StatusCode.Cancelled, "The call was cancelled."));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, LogTemplate, context.Method, context.Peer, StatusCode.Internal, stopwatch.ElapsedMilliseconds);
            activity?.SetTag("rpc.grpc.status_code", (int)StatusCode.Internal);
            throw new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
        }
    }

    private static void TagRpc(Activity? activity, string fullMethod)
    {
        if (activity is null)
            return;

        // The method has the form "/package.Service/Method".
        var parts = fullMethod.Split('/', StringSplitOptions.RemoveEmptyEntries);
        activity.SetTag("rpc.system", "grpc");
        if (parts.Length == 2)
        {
            activity.SetTag("rpc.service", parts[0]);
            activity.SetTag("rpc.method", parts[1]);
        }
    }
}
