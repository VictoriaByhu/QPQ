using System.Diagnostics;
using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Messages.Application.Common;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Messages.Application.Messages.Commands.SendFileMessage;

public sealed class SendFileMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IAttachmentRepository attachments,
    IMapper mapper,
    ILogger<SendFileMessageCommandHandler> logger) : IRequestHandler<SendFileMessageCommand, MessageDto>
{
    private static readonly ActivitySource Source = new("Messages.WebApi");

    public async Task<MessageDto> Handle(SendFileMessageCommand command, CancellationToken ct)
    {
        using var activity = Source.StartActivity("SendFileMessage");
        activity?.SetTag("conversation.id", command.ConversationId);
        activity?.SetTag("user.id", command.UserId);

        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);

        var ids = command.AttachmentIds!.Select(ObjectId.Parse).ToList();
        var files = (await attachments.GetByIdsAsync(conversation.Id, ids, ct)).ToDictionary(a => a.Id);
        var missing = ids.Where(id => !files.ContainsKey(id)).ToList();
        if (missing.Count > 0)
            throw new NotFoundException($"Attachments not found in this conversation: {string.Join(", ", missing)}.");

        var message = new FileMessage(conversation.Id, command.UserId, command.Caption, ids.Select(id => files[id].ToRef()));
        conversation.RegisterMessage(message);

        await messages.AddAsync(message, ct);
        await conversations.UpdateAsync(conversation, ct);

        activity?.SetTag("message.id", message.Id.ToString());
        activity?.SetTag("swap.id", conversation.SwapId);
        logger.LogInformation("Message {MessageId} of type {MessageType} sent to conversation {ConversationId} of swap {SwapId} by {UserId}",
            message.Id.ToString(), "file", conversation.Id.ToString(), conversation.SwapId, command.UserId);

        return mapper.Map<MessageDto>(message);
    }
}
