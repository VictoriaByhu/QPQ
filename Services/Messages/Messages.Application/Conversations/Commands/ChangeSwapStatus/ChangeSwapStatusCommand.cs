using Messages.Application.Common.Messaging;

namespace Messages.Application.Conversations.Commands.ChangeSwapStatus;

public sealed record ChangeSwapStatusCommand(
    string ConversationId, string UserId, string? Status, int? ExpectedVersion) : ICommand;
