using Messages.Application.Common.Messaging;

namespace Messages.Application.Messages.Commands.EditTextMessage;

public sealed record EditTextMessageCommand(string MessageId, string UserId, string? Text, int? ExpectedVersion) : ICommand;
