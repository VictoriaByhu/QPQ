using Messages.Application.Common.Messaging;

namespace Messages.Application.Conversations.Commands.DeleteConversation;

public sealed record DeleteConversationCommand(string ConversationId, string UserId, int? ExpectedVersion) : ICommand;
