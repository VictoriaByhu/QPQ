using FluentValidation;
using Messages.Application.Common.Validation;
using Messages.Domain.Entities;

namespace Messages.Application.Messages.Commands.EditTextMessage;

public sealed class EditTextMessageCommandValidator : AbstractValidator<EditTextMessageCommand>
{
    public EditTextMessageCommandValidator()
    {
        RuleFor(x => x.MessageId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(TextMessage.TextMaxLength);
        RuleFor(x => x.ExpectedVersion).MustBeVersion();
    }
}
