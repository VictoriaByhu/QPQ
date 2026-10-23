using QPQ.Moderation.Domain.Exceptions;
using QPQ.Moderation.Domain.Entities; // Підключаємо старі сутності (наприклад, ReportDetail)

namespace QPQ.Moderation.Domain;

public class Report
{
    public long Id { get; private set; }

    // Повернуто поле Title для конфігурації
    public string Title { get; private set; } = string.Empty;

    public long TargetUserId { get; private set; }
    public string TargetUserDisplayName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public long? AssignedModeratorId { get; private set; }
    public string FullDescription { get; private set; } = string.Empty;

    // Повернуто зв'язок 1:1 для конфігурації
    public ReportDetail? Detail { get; private set; }

    // Додані навігаційні властивості для зв'язків
    public List<ReportAttachment> Attachments { get; private set; } = new();
    public List<ReportTag> ReportTags { get; private set; } = new();

    private Report() { }

    // Конструктор тепер приймає Title (за замовчуванням "Нова скарга")
    public Report(long targetUserId, string targetUserDisplayName, string fullDescription, string title = "Нова скарга")
    {
        if (targetUserId <= 0)
            throw new BusinessConflictException("ID користувача має бути більшим за нуль.");

        if (string.IsNullOrWhiteSpace(targetUserDisplayName))
            throw new BusinessConflictException("Ім'я користувача не може бути порожнім.");

        TargetUserId = targetUserId;
        TargetUserDisplayName = targetUserDisplayName;
        FullDescription = fullDescription;
        Title = title;
        Status = "Pending";
    }

    public void AssignModerator(long moderatorId)
    {
        if (moderatorId <= 0)
            throw new BusinessConflictException("Некоректний ID модератора.");

        AssignedModeratorId = moderatorId;
        Status = "In Review";
    }

    public void UpdateStatus(string newStatus)
    {
        if (string.IsNullOrWhiteSpace(newStatus))
            throw new BusinessConflictException("Статус не може бути порожнім.");

        Status = newStatus;
    }
}