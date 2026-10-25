using MediatR;
using MongoDB.Bson;
using Messages.Application.Common.Interfaces;
using Messages.Application.Messages.Dtos;
using Messages.Domain.Exceptions;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Queries.GetMessageById;

public sealed class GetMessageByIdQueryHandler(IMessageQueries queries, IConversationRepository conversations)
    : IRequestHandler<GetMessageByIdQuery, MessageDto>
{
    public async Task<MessageDto> Handle(GetMessageByIdQuery query, CancellationToken ct)
    {
        var message = await queries.GetMessageAsync(query.Id, ct);
        if (message is null
            || !await conversations.IsParticipantAsync(ObjectId.Parse(message.ConversationId), query.UserId, ct))
            throw new NotFoundException($"Message {query.Id} was not found.");
        return message;
    }
}
