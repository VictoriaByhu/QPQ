using Messages.Application.Common.Messaging;
using Messages.Application.Messages.Dtos;

namespace Messages.Application.Messages.Queries.GetMessageById;

public sealed record GetMessageByIdQuery(string Id, string UserId) : IQuery<MessageDto>;
