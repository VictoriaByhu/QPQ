namespace Messages.WebApi.Contracts;

public sealed record CreateConversationRequest(int SwapId, string? InitiatorId, string? PartnerId);

public sealed record ChangeSwapStatusRequest(string? Status);

public sealed record SendTextMessageRequest(string? Text);

public sealed record SendFileMessageRequest(string? Caption, IReadOnlyList<string>? AttachmentIds);

public sealed record EditMessageRequest(string? Text);

public sealed record UploadAttachmentRequest(string? FileName, string? Url, string? ContentType, long SizeBytes);
