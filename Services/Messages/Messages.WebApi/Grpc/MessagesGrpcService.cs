using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Messages.Application.Conversations.Dtos;
using Messages.Application.Conversations.Queries.GetSwapConversation;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Exceptions;
using Messages.Grpc;
using Platform.ServiceDefaults.Grpc;

namespace Messages.WebApi.Grpc;

public sealed class MessagesGrpcService(ISender sender) : MessagesService.MessagesServiceBase
{
    public override async Task<SwapConversationReply> GetSwapConversation(
        GetSwapConversationRequest request, ServerCallContext context)
    {
        if (request.SwapId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Swap id must be positive."));

        var userId = GrpcCaller.GetRequiredUserId(context);

        try
        {
            var query = new GetSwapConversationQuery(request.SwapId, userId.ToString("D"));
            return ToReply(await sender.Send(query, context.CancellationToken));
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

    private static SwapConversationReply ToReply(SwapConversationDto result)
    {
        var reply = new SwapConversationReply { Conversation = ToReply(result.Conversation) };
        reply.Messages.AddRange(result.Messages.Select(ToReply));
        return reply;
    }

    private static ConversationReply ToReply(ConversationDto conversation)
    {
        var reply = new ConversationReply
        {
            Id = conversation.Id,
            SwapId = conversation.SwapId,
            SwapStatus = conversation.SwapStatus,
            CreatedAt = ToTimestamp(conversation.CreatedAt),
            UpdatedAt = ToTimestamp(conversation.UpdatedAt),
            Version = conversation.Version
        };

        reply.Participants.AddRange(conversation.Participants.Select(p => new ParticipantReply
        {
            UserId = p.UserId,
            Role = p.Role,
            LastReadAt = ToTimestamp(p.LastReadAt)
        }));
        return reply;
    }

    private static MessageReply ToReply(MessageDto message)
    {
        var reply = new MessageReply
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = message.SenderId,
            Type = message.Type,
            CreatedAt = ToTimestamp(message.CreatedAt),
            EditedAt = ToTimestamp(message.EditedAt),
            Version = message.Version
        };

        if (message.Text is not null)
            reply.Text = message.Text;
        if (message.Caption is not null)
            reply.Caption = message.Caption;
        if (message.Event is not null)
            reply.Event = message.Event;

        reply.Attachments.AddRange(message.Attachments.Select(a => new AttachmentRefReply
        {
            AttachmentId = a.AttachmentId,
            FileName = a.FileName
        }));
        return reply;
    }

    private static Timestamp? ToTimestamp(DateTime? value)
        => value is { } date ? ToTimestamp(date) : null;

    private static Timestamp ToTimestamp(DateTime value)
        => Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
