namespace QPQ.Moderation.Domain.Entities;

public class ReportDetail
{
    public long ReportId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SystemLogs { get; set; }
}