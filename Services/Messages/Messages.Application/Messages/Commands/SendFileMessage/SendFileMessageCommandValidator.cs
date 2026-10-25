using FluentValidation;
using MongoDB.Bson;
using Messages.Application.Common.Validation;
using Messages.Domain.Entities;
using Messages.Domain.Interfaces;

namespace Messages.Application.Messages.Commands.SendFileMessage;

public sealed class SendFileMessageCommandValidator : AbstractValidator<SendFileMessageCommand>
{
    public SendFileMessageCommandValidator(IAttachmentRepository attachments)
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.Caption).MaximumLength(FileMessage.CaptionMaxLength);

        RuleFor(x => x.AttachmentIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(ids => ids!.Count <= FileMessage.MaxAttachments)
            .WithMessage($"A file message cannot contain more than {FileMessage.MaxAttachments} attachments.")
            .Must(ids => ids!.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids!.Count)
            .WithMessage("Attachments must not repeat.");
        RuleForEach(x => x.AttachmentIds).MustBeObjectId();

        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                var ids = cmd.AttachmentIds!.Select(ObjectId.Parse).ToList();
                var found = await attachments.CountInConversationAsync(ObjectId.Parse(cmd.ConversationId), ids, ct);
                return found == ids.Count;
            })
            .When(HasWellFormedIds)
            .OverridePropertyName(nameof(SendFileMessageCommand.AttachmentIds))
            .WithMessage("All attachments must be uploaded to this conversation first.");
    }

    private static bool HasWellFormedIds(SendFileMessageCommand cmd)
        => RuleBuilderExtensions.IsObjectId(cmd.ConversationId)
           && cmd.AttachmentIds is { Count: > 0 and <= FileMessage.MaxAttachments } ids
           && ids.All(RuleBuilderExtensions.IsObjectId)
           && ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() == ids.Count;
}
