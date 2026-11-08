using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Platform.ServiceDefaults.Grpc;
using Sessions.Bll.Dtos;
using Sessions.Bll.Services;
using Sessions.Domain.Exceptions;
using Sessions.Grpc;

namespace Sessions.Api.Grpc;

public sealed class SessionsGrpcService(IAcceptedSwapService swaps) : SessionsService.SessionsServiceBase
{
    public override async Task<SessionsBySwapReply> GetSessionsBySwap(
        GetSessionsBySwapRequest request, ServerCallContext context)
    {
        if (request.SwapId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Swap id must be positive."));

        var userId = GrpcCaller.GetRequiredUserId(context);

        try
        {
            return ToReply(await swaps.GetWithSessionsAsync(userId, request.SwapId, context.CancellationToken));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private static SessionsBySwapReply ToReply(AcceptedSwapDto swap)
    {
        var reply = new SessionsBySwapReply
        {
            SwapId = swap.SwapId,
            InitiatorId = swap.InitiatorId.ToString("D"),
            PartnerId = swap.PartnerId.ToString("D"),
            Status = swap.Status
        };

        reply.Sessions.AddRange(swap.Sessions.Select(ToReply));
        return reply;
    }

    private static SessionReply ToReply(SessionListItemDto session) => new()
    {
        Id = session.Id,
        SwapId = session.SwapId,
        FormatCode = session.FormatCode,
        // Dates are stored without an offset; Timestamp only accepts UTC, so the value is carried over unchanged.
        ScheduledAt = Timestamp.FromDateTime(DateTime.SpecifyKind(session.ScheduledAt, DateTimeKind.Utc)),
        DurationMinutes = session.DurationMinutes,
        Status = session.Status,
        RowVersion = session.RowVersion
    };
}
