using Messages.Application.Common.Messaging;
using Messages.Application.Messages.Dtos;

namespace Messages.Application.Messages.Commands.SendTextMessage;

public sealed record SendTextMessageCommand(string ConversationId, string UserId, string? Text) : ICommand<MessageDto>;
