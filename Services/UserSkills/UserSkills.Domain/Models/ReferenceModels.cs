namespace UserSkills.Domain.Models;

/// <summary>Локальна копія довідника навичок з Catalog (власник даних: Catalog).</summary>
public class Skill
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}

/// <summary>Мова, якою користувач може проводити навчання (власник: UserSkills).</summary>
public class Language
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
}
