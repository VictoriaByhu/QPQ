namespace QPQ.Moderation.Domain.Entities;

public class ReportAttachment
{
    public long Id { get; set; }
    public long ReportId { get; set; }
    public string FileUrl { get; set; } = string.Empty;
}