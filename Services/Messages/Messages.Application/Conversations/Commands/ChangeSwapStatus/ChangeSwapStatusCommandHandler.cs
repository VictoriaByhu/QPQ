using MediatR;
using Messages.Application.Common;
using Messages.Domain.Enums;
using Messages.Domain.Interfaces;

namespace Messages.Application.Conversations.Commands.ChangeSwapStatus;

public sealed class ChangeSwapStatusCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages) : IRequestHandler<ChangeSwapStatusCommand>
{
    public async Task Handle(ChangeSwapStatusCommand command, CancellationToken ct)
    {
        var conversation = await conversations.GetForParticipantAsync(command.ConversationId, command.UserId, ct);
        conversation.EnsureVersion(command.ExpectedVersion);

        var status = Enum.Parse<SwapStatus>(command.Status!, true);
        var systemMessage = conversation.ChangeSwapStatus(status, command.UserId);

        await conversations.UpdateAsync(conversation, ct);
        await messages.AddAsync(systemMessage, ct);
    }
}
