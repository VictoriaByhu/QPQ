using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Messages.Queries.GetMessageById;

public sealed class GetMessageByIdQueryValidator : AbstractValidator<GetMessageByIdQuery>
{
    public GetMessageByIdQueryValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
    }
}
