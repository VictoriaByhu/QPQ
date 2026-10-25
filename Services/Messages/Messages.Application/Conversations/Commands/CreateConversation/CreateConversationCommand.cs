using Messages.Application.Common.Messaging;
using Messages.Application.Conversations.Dtos;

namespace Messages.Application.Conversations.Commands.CreateConversation;

public sealed record CreateConversationCommand(
    string UserId, int SwapId, string InitiatorId, string PartnerId) : ICommand<ConversationDto>;
