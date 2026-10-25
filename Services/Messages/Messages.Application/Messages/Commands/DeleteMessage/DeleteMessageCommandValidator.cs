using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Messages.Commands.DeleteMessage;

public sealed class DeleteMessageCommandValidator : AbstractValidator<DeleteMessageCommand>
{
    public DeleteMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.ExpectedVersion).MustBeVersion();
    }
}
