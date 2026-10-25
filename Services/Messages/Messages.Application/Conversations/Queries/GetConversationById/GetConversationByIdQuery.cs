using Messages.Application.Common.Messaging;
using Messages.Application.Conversations.Dtos;

namespace Messages.Application.Conversations.Queries.GetConversationById;

public sealed record GetConversationByIdQuery(string Id, string UserId) : IQuery<ConversationDto>;
