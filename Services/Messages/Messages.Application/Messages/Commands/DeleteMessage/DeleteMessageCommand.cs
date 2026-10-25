using Messages.Application.Common.Messaging;

namespace Messages.Application.Messages.Commands.DeleteMessage;

public sealed record DeleteMessageCommand(string MessageId, string UserId, int? ExpectedVersion) : ICommand;
