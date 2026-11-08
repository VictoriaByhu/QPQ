using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Conversations.Queries.GetSwapConversation;

public sealed class GetSwapConversationQueryValidator : AbstractValidator<GetSwapConversationQuery>
{
    public GetSwapConversationQueryValidator()
    {
        RuleFor(x => x.SwapId).GreaterThan(0);
        RuleFor(x => x.UserId).MustBeUserId();
    }
}
