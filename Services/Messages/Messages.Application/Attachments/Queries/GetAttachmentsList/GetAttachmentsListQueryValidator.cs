using FluentValidation;
using Messages.Application.Common.Validation;

namespace Messages.Application.Attachments.Queries.GetAttachmentsList;

public sealed class GetAttachmentsListQueryValidator : AbstractValidator<GetAttachmentsListQuery>
{
    public GetAttachmentsListQueryValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Page).MustBePage();
        RuleFor(x => x.PageSize).MustBePageSize();
    }
}
