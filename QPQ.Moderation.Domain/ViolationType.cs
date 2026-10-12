namespace QPQ.Moderation.Domain;

public class ViolationType
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SeverityLevel { get; set; }
    public string Description { get; set; } = string.Empty;
}