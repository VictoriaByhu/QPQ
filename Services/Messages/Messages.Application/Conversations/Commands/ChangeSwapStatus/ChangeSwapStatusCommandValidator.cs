using FluentValidation;
using Messages.Application.Common.Validation;
using Messages.Domain.Enums;

namespace Messages.Application.Conversations.Commands.ChangeSwapStatus;

public sealed class ChangeSwapStatusCommandValidator : AbstractValidator<ChangeSwapStatusCommand>
{
    public ChangeSwapStatusCommandValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Status).NotEmpty().MustBeEnum(SwapStatus.Completed, SwapStatus.Cancelled);
        RuleFor(x => x.ExpectedVersion).MustBeVersion();
    }
}
