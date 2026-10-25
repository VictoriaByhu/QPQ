using AutoMapper;
using MediatR;
using Messages.Application.Conversations.Dtos;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Application.Conversations.Commands.CreateConversation;

public sealed class CreateConversationCommandHandler(
    IConversationRepository conversations,
    IMessageRepository messages,
    IMapper mapper) : IRequestHandler<CreateConversationCommand, ConversationDto>
{
    public async Task<ConversationDto> Handle(CreateConversationCommand command, CancellationToken ct)
    {
        var conversation = new Conversation(command.SwapId, command.InitiatorId, command.PartnerId);
        var accepted = conversation.CreateStatusMessage(command.UserId);

        await conversations.AddAsync(conversation, ct);
        await messages.AddAsync(accepted, ct);

        return mapper.Map<ConversationDto>(conversation);
    }
}
