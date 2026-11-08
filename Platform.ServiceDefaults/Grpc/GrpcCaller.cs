using Grpc.Core;

namespace Platform.ServiceDefaults.Grpc;

public static class GrpcCaller
{
    public const string UserIdMetadataKey = "x-user-id";

    /// <summary>Reads the participant id from the call metadata, the gRPC counterpart of the X-User-Id header.</summary>
    public static Guid GetRequiredUserId(ServerCallContext context)
    {
        var value = context.RequestHeaders.GetValue(UserIdMetadataKey);
        if (Guid.TryParse(value, out var userId) && userId != Guid.Empty)
            return userId;

        throw new RpcException(new Status(StatusCode.Unauthenticated,
            $"Metadata '{UserIdMetadataKey}' must contain a non-empty GUID."));
    }
}
