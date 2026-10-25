using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Conversations.Commands.MarkConversationRead;

public sealed class MarkConversationReadCommandValidator : AbstractValidator<MarkConversationReadCommand>
{
    public MarkConversationReadCommandValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
    }
}
