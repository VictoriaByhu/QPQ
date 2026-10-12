namespace QPQ.Moderation.Bll.DTOs;

public record ReportDto(
    long Id,
    long TargetUserId,
    string TargetUserDisplayName,
    string Status,
    string FullDescription
);