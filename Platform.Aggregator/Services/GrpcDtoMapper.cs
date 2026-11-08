using Google.Protobuf.WellKnownTypes;
using Messages.Grpc;
using Platform.Aggregator.Dtos;
using Sessions.Grpc;
using Swaps.Grpc;

namespace Platform.Aggregator.Services;

internal static class GrpcDtoMapper
{
    public static SwapDto ToDto(this SwapReply reply) => new(
        reply.Id,
        Guid.Parse(reply.InitiatorId),
        Guid.Parse(reply.PartnerId),
        reply.Status,
        reply.CreatedAt.ToLocalClock(),
        reply.UpdatedAt?.ToLocalClock(),
        reply.Details?.ToDto(),
        reply.Skills.Select(s => new SwapSkillDto(s.SkillId, s.SkillName, s.Role)).ToList());

    public static IReadOnlyList<SessionDto> ToDtos(this SessionsBySwapReply reply)
        => reply.Sessions.Select(s => new SessionDto(
            s.Id,
            s.SwapId,
            s.FormatCode,
            s.ScheduledAt.ToLocalClock(),
            s.DurationMinutes,
            s.Status)).ToList();

    public static ConversationMessagesDto ToDto(this SwapConversationReply reply) => new(
        new ConversationDto(reply.Conversation.Id, reply.Conversation.SwapId, reply.Conversation.SwapStatus),
        reply.Messages.Select(m => new MessageDto(
            m.Id,
            m.ConversationId,
            m.SenderId,
            m.Type,
            m.HasText ? m.Text : null,
            m.HasCaption ? m.Caption : null,
            m.HasEvent ? m.Event : null,
            m.CreatedAt.ToDateTime(),
            m.EditedAt?.ToDateTime())).ToList());

    private static SwapDetailsDto ToDto(this SwapDetailsReply details) => new(
        details.HasMessage ? details.Message : null,
        details.HasLocation ? details.Location : null,
        details.ProposedDate?.ToLocalClock(),
        details.HasDurationMinutes ? details.DurationMinutes : null);

    // Swaps and Sessions store dates without an offset, so the JSON must not gain a "Z" suffix.
    private static DateTime ToLocalClock(this Timestamp timestamp)
        => DateTime.SpecifyKind(timestamp.ToDateTime(), DateTimeKind.Unspecified);
}
