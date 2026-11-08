using Messages.Application.Common.Messaging;
using Messages.Application.Conversations.Dtos;

namespace Messages.Application.Conversations.Queries.GetSwapConversation;

public sealed record GetSwapConversationQuery(int SwapId, string UserId) : IQuery<SwapConversationDto>;
