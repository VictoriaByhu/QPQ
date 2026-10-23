using QPQ.Moderation.Domain.Exceptions;

namespace QPQ.Moderation.Domain;

public class Tag
{
    public long Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Зв'язок 1:N до проміжної сутності
    public List<ReportTag> ReportTags { get; private set; } = new();

    private Tag() { }

    public Tag(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new BusinessConflictException("Назва тегу не може бути порожньою.");
        Name = name;
    }
}