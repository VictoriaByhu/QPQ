namespace QPQ.Moderation.Domain.Entities;

public class ReportAssignment
{
    public long ReportId { get; set; }
    public Report Report { get; set; } = null!;

    public long ModeratorId { get; set; }
    public Moderator Moderator { get; set; } = null!;

    public DateTime AssignedAt { get; set; }
}