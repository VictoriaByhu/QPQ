using MediatR;
using Messages.Application.Common;
using Messages.Domain.Entities;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Commands.EditTextMessage;

public sealed class EditTextMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages) : IRequestHandler<EditTextMessageCommand>
{
    public async Task Handle(EditTextMessageCommand command, CancellationToken ct)
    {
        var message = await messages.GetVisibleAsync(conversations, command.MessageId, command.UserId, ct);
        if (message is not TextMessage textMessage)
            throw new DomainException($"Only text messages can be edited, this one is '{message.Type}'.");

        textMessage.EnsureVersion(command.ExpectedVersion);
        textMessage.Edit(command.UserId, command.Text!);
        await messages.UpdateAsync(textMessage, ct);

        var conversation = await conversations.GetByIdAsync(textMessage.ConversationId, ct);
        if (conversation is not null && conversation.SyncLastMessage(textMessage.Id, textMessage))
            await conversations.UpdateAsync(conversation, ct);
    }
}
