using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Messages.Queries.GetMessagesList;

public sealed class GetMessagesListQueryValidator : AbstractValidator<GetMessagesListQuery>
{
    public const int SearchMaxLength = 100;

    public GetMessagesListQueryValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Search).MaximumLength(SearchMaxLength);
        RuleFor(x => x.SenderId!).MustBeUserId().When(x => x.SenderId is not null);
        RuleFor(x => x.Page).MustBePage();
        RuleFor(x => x.PageSize).MustBePageSize();
    }
}
