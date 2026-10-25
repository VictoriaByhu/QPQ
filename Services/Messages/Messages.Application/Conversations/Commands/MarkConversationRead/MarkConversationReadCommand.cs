using Messages.Application.Common.Messaging;

namespace Messages.Application.Conversations.Commands.MarkConversationRead;

public sealed record MarkConversationReadCommand(string ConversationId, string UserId) : ICommand;
