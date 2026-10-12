namespace QPQ.Moderation.Domain;

public class Report
{
    public long Id { get; set; }
    public long TargetUserId { get; set; }
    public string TargetUserDisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public long? AssignedModeratorId { get; set; }

    // Поле з таблиці ReportDetails (для демонстрації JOIN-запиту)
    public string FullDescription { get; set; } = string.Empty;
}