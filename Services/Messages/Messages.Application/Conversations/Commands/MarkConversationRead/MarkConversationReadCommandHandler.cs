using MediatR;
using Messages.Application.Common;
using Messages.Domain.Interfaces;

namespace Messages.Application.Conversations.Commands.MarkConversationRead;

public sealed class MarkConversationReadCommandHandler(IConversationRepository conversations)
    : IRequestHandler<MarkConversationReadCommand>
{
    public async Task Handle(MarkConversationReadCommand command, CancellationToken ct)
    {
        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);
        conversation.MarkRead(command.UserId, DateTime.UtcNow);
        await conversations.UpdateAsync(conversation, ct);
    }
}
