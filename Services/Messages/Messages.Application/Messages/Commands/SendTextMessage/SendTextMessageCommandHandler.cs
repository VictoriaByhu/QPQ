using AutoMapper;
using MediatR;
using Messages.Application.Common;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Commands.SendTextMessage;

public sealed class SendTextMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IMapper mapper) : IRequestHandler<SendTextMessageCommand, MessageDto>
{
    public async Task<MessageDto> Handle(SendTextMessageCommand command, CancellationToken ct)
    {
        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);

        var message = new TextMessage(conversation.Id, command.UserId, command.Text!);
        conversation.RegisterMessage(message);

        await messages.AddAsync(message, ct);
        await conversations.UpdateAsync(conversation, ct);

        return mapper.Map<MessageDto>(message);
    }
}
