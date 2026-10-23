namespace QPQ.Moderation.Domain;

public class ReportTag
{
    public long ReportId { get; private set; }
    public Report Report { get; private set; } = null!;

    public long TagId { get; private set; }
    public Tag Tag { get; private set; } = null!;

    // Власне додаткове поле, як вимагає викладач
    public DateTime AssignedAt { get; private set; }

    private ReportTag() { }

    public ReportTag(long reportId, long tagId)
    {
        ReportId = reportId;
        TagId = tagId;
        AssignedAt = DateTime.UtcNow; // Фіксуємо час призначення тегу
    }
}