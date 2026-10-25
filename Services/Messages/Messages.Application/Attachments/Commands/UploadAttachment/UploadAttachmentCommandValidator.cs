using FluentValidation;
using Messages.Application.Common.Validation;
using Messages.Domain.Entities;
using Messages.Domain.ValueObjects;

namespace Messages.Application.Attachments.Commands.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private static readonly char[] ForbiddenFileNameChars = ['/', '\\', ':', '*', '?', '"', '<', '>', '|'];

    public UploadAttachmentCommandValidator()
    {
        RuleFor(x => x.ConversationId).MustBeObjectId();
        RuleFor(x => x.UserId).MustBeUserId();
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(AttachmentRef.FileNameMaxLength)
            .Must(n => n is null || n.IndexOfAny(ForbiddenFileNameChars) < 0)
            .WithMessage("'{PropertyName}' must not contain path characters.");
        RuleFor(x => x.Url).NotEmpty().MaximumLength(Attachment.UrlMaxLength)
            .Must(BeHttpUrl).WithMessage("'{PropertyName}' must be an absolute http(s) URL.");
        RuleFor(x => x.ContentType).NotEmpty().MaximumLength(Attachment.ContentTypeMaxLength)
            .Matches(@"^[\w.+-]+/[\w.+-]+$").WithMessage("'{PropertyName}' must look like 'type/subtype'.");
        RuleFor(x => x.SizeBytes).InclusiveBetween(0, Attachment.MaxSizeBytes);
    }

    private static bool BeHttpUrl(string? url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
