using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Platform.ServiceDefaults.Grpc;
using Swaps.Bll.Caching;
using Swaps.Bll.Dtos;
using Swaps.Bll.Services;
using Swaps.Grpc;

namespace Swaps.Api.Grpc;

public sealed class SwapsGrpcService(ISwapCardReader cards, ISkillService skills) : SwapsService.SwapsServiceBase
{
    public override async Task<SwapReply> GetSwap(GetSwapRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Swap id must be positive."));

        var userId = GrpcCaller.GetRequiredUserId(context);

        var swap = await cards.GetCardAsync(request.Id, context.CancellationToken)
                   ?? throw new RpcException(new Status(StatusCode.NotFound, $"Swap {request.Id} not found."));

        if (swap.InitiatorId != userId && swap.PartnerId != userId)
            throw new RpcException(new Status(StatusCode.PermissionDenied, "The user is not a participant of this swap."));

        return ToReply(swap);
    }

    public override async Task<SkillsReply> GetSkillsByIds(GetSkillsByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count == 0 || request.Ids.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Ids must be a non-empty list of positive numbers."));

        var requested = request.Ids.ToHashSet();
        var all = await skills.GetAllAsync(context.CancellationToken);

        var reply = new SkillsReply();
        reply.Skills.AddRange(all
            .Where(s => requested.Contains(s.Id))
            .Select(s => new SkillReply { Id = s.Id, Name = s.Name }));
        return reply;
    }

    private static SwapReply ToReply(SwapDto swap)
    {
        var reply = new SwapReply
        {
            Id = swap.Id,
            InitiatorId = swap.InitiatorId.ToString("D"),
            PartnerId = swap.PartnerId.ToString("D"),
            Status = swap.Status,
            CreatedAt = ToTimestamp(swap.CreatedAt),
            UpdatedAt = swap.UpdatedAt is { } updatedAt ? ToTimestamp(updatedAt) : null,
            RowVersion = swap.RowVersion,
            Details = swap.Details is null ? null : ToReply(swap.Details)
        };

        reply.Skills.AddRange(swap.Skills.Select(s => new SwapSkillReply
        {
            SkillId = s.SkillId,
            SkillName = s.SkillName,
            Role = s.Role
        }));
        return reply;
    }

    private static SwapDetailsReply ToReply(SwapDetailsDto details)
    {
        var reply = new SwapDetailsReply
        {
            ProposedDate = details.ProposedDate is { } proposedDate ? ToTimestamp(proposedDate) : null
        };

        if (details.Message is not null)
            reply.Message = details.Message;
        if (details.Location is not null)
            reply.Location = details.Location;
        if (details.DurationMinutes is { } duration)
            reply.DurationMinutes = duration;
        return reply;
    }

    // Dates are stored without an offset; Timestamp only accepts UTC, so the value is carried over unchanged.
    private static Timestamp ToTimestamp(DateTime value)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
