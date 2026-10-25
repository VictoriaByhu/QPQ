using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Attachments.Queries.GetAttachmentById;

public sealed class GetAttachmentByIdQueryValidator : AbstractValidator<GetAttachmentByIdQuery>
{
    public GetAttachmentByIdQueryValidator()
    {
        RuleFor(x => x.Id).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
    }
}
