using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Conversations.Queries.GetConversationById;

public sealed class GetConversationByIdQueryValidator : AbstractValidator<GetConversationByIdQuery>
{
    public GetConversationByIdQueryValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
    }
}
