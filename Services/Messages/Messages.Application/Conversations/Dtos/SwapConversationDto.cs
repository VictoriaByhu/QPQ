using Messages.Application.Messages.Dtos;

namespace Messages.Application.Conversations.Dtos;

public sealed record SwapConversationDto(ConversationDto Conversation, IReadOnlyList<MessageDto> Messages);
