using MediatR;
using Messages.Application.Common;
using Messages.Domain.Interfaces;

namespace Messages.Application.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IAttachmentRepository attachments) : IRequestHandler<DeleteConversationCommand>
{
    public async Task Handle(DeleteConversationCommand command, CancellationToken ct)
    {
        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);
        conversation.EnsureVersion(command.ExpectedVersion);
        conversation.EnsureCanBeDeleted();

        await conversations.DeleteAsync(conversation, ct);
        await messages.DeleteByConversationAsync(conversation.Id, ct);
        await attachments.DeleteByConversationAsync(conversation.Id, ct);
    }
}
