using QPQ.Moderation.Domain.Entities;

namespace QPQ.Moderation.Domain;

public class Moderator
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public List<ReportAssignment> Assignments { get; set; } = new();
}