using Messages.Application.Common.Messaging;
using Messages.Application.Messages.Dtos;

namespace Messages.Application.Messages.Commands.SendFileMessage;

public sealed record SendFileMessageCommand(
    string ConversationId, string UserId, string? Caption, IReadOnlyList<string>? AttachmentIds)
    : ICommand<MessageDto>;
