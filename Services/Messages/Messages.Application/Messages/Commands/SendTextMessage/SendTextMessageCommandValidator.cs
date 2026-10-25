using FluentValidation;
using Messages.Application.Common.Validation;
using Messages.Domain.Entities;

namespace Messages.Application.Messages.Commands.SendTextMessage;

public sealed class SendTextMessageCommandValidator : AbstractValidator<SendTextMessageCommand>
{
    public SendTextMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Text).NotEmpty().MaximumLength(TextMessage.TextMaxLength);
    }
}
