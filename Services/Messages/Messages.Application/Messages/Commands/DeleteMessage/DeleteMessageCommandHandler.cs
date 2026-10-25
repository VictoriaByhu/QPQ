using MediatR;
using Messages.Application.Common;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Commands.DeleteMessage;

public sealed class DeleteMessageCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages) : IRequestHandler<DeleteMessageCommand>
{
    public async Task Handle(DeleteMessageCommand command, CancellationToken ct)
    {
        var message = await messages.GetVisibleAsync(conversations, command.MessageId, command.UserId, ct);
        message.EnsureVersion(command.ExpectedVersion);
        message.Delete(command.UserId);
        await messages.UpdateAsync(message, ct);

        var conversation = await conversations.GetByIdAsync(message.ConversationId, ct);
        if (conversation is null)
            return;

        var latest = await messages.GetLatestVisibleAsync(conversation.Id, ct);
        if (conversation.SyncLastMessage(message.Id, latest))
            await conversations.UpdateAsync(conversation, ct);
    }
}
