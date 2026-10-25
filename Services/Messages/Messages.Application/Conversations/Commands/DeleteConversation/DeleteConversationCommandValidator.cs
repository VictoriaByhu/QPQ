using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Conversations.Commands.DeleteConversation;

public sealed class DeleteConversationCommandValidator : AbstractValidator<DeleteConversationCommand>
{
    public DeleteConversationCommandValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.ExpectedVersion).MustBeVersion();
    }
}
