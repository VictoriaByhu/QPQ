using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Messages.Application.Common;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Commands.SendFileMessage;

public sealed class SendFileMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IAttachmentRepository attachments,
    IMapper mapper) : IRequestHandler<SendFileMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendFileMessageCommand command, CancellationToken ct)
    {
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

        return mapper.Map<MessageDto>(message);
    }
}
