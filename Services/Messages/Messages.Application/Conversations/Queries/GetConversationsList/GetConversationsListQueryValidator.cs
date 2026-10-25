using FluentValidation;
using Messages.Application.Common.Validation;
using Messages.Domain.Enums;

namespace Messages.Application.Conversations.Queries.GetConversationsList;

public sealed class GetConversationsListQueryValidator : AbstractValidator<GetConversationsListQuery>
{
    public GetConversationsListQueryValidator()
    {
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.SwapStatus).MustBeEnum<GetConversationsListQuery, SwapStatus>();
        RuleFor(x => x.Page).MustBePage();
        RuleFor(x => x.PageSize).MustBePageSize();
    }
}
