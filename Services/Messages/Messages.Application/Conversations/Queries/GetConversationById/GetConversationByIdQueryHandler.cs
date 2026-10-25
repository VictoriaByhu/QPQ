using MediatR;
using Messages.Application.Common.Interfaces;
using Messages.Application.Conversations.Dtos;
using Messages.Domain.Exceptions;

namespace Messages.Application.Conversations.Queries.GetConversationById;

public sealed class GetConversationByIdQueryHandler(IMessageQueries queries)
    : IRequestHandler<GetConversationByIdQuery, ConversationDto>
{
    public async Task<ConversationDto> Handle(GetConversationByIdQuery query, CancellationToken ct)
        => await queries.GetConversationAsync(query.Id, query.UserId, ct)
           ?? throw new NotFoundException($"Conversation {query.Id} was not found.");
}
