using System.Diagnostics;
using AutoMapper;
using MediatR;
using Messages.Application.Common;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messages.Application.Messages.Commands.SendTextMessage;

public sealed class SendTextMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IMapper mapper,
    ILogger<SendTextMessageCommandHandler> logger) : IRequestHandler<SendTextMessageCommand, MessageDto>
{
    private static readonly ActivitySource Source = new("Messages.WebApi");

    public async Task<MessageDto> Handle(SendTextMessageCommand command, CancellationToken ct)
    {
        using var activity = Source.StartActivity("SendTextMessage");
        activity?.SetTag("conversation.id", command.ConversationId);
        activity?.SetTag("user.id", command.UserId);

        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);

        var message = new TextMessage(conversation.Id, command.UserId, command.Text!);
        conversation.RegisterMessage(message);

        await messages.AddAsync(message, ct);
        await conversations.UpdateAsync(conversation, ct);

        activity?.SetTag("message.id", message.Id.ToString());
        activity?.SetTag("swap.id", conversation.SwapId);
        logger.LogInformation("Message {MessageId} of type {MessageType} sent to conversation {ConversationId} of swap {SwapId} by {UserId}",
            message.Id.ToString(), "text", conversation.Id.ToString(), conversation.SwapId, command.UserId);

        return mapper.Map<MessageDto>(message);
    }
}
