using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Conversations.Commands.CreateConversation;

public sealed class CreateConversationCommandValidator : AbstractValidator<CreateConversationCommand>
{
    public CreateConversationCommandValidator()
    {
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.SwapId).GreaterThan(0);
        RuleFor(x => x.InitiatorId).MustBeUserId();
        RuleFor(x => x.PartnerId).MustBeUserId()
            .Must((cmd, partnerId) => !SameUser(cmd.InitiatorId, partnerId))
            .WithMessage("Initiator and partner must be different users.");
        RuleFor(x => x.UserId)
            .Must((cmd, userId) => SameUser(userId, cmd.InitiatorId) || SameUser(userId, cmd.PartnerId))
            .When(x => RuleBuilderExtensions.IsUserId(x.UserId))
            .WithMessage("The current user must be the initiator or the partner of the swap.");
    }

    private static bool SameUser(string? left, string? right)
        => Guid.TryParse(left, out var a) && Guid.TryParse(right, out var b) && a == b;
}
