using Sessions.Domain.Exceptions;

namespace Sessions.Domain.Models;

public class Skill
{
    public const int NameMaxLength = 100;

    public int Id { get; private set; }
    public string Name { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private Skill() { }

    public Skill(int id, string name)
    {
        if (id <= 0)
            throw new DomainException("Skill id must be positive.");

        Id = id;
        SetName(name);
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        SetName(name);
        UpdatedAt = DateTime.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > NameMaxLength)
            throw new DomainException($"Skill name is required and must not exceed {NameMaxLength} characters.");

        Name = name.Trim();
    }
}
