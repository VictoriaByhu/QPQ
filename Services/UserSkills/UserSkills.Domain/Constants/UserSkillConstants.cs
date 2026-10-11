namespace UserSkills.Domain.Constants;

public static class UserSkillTypes
{
    public const string Offer = "Offer";
    public const string Want = "Want";

    public static bool IsValid(string? value) => value is Offer or Want;
}

public static class UserSkillStatuses
{
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Archived = "Archived";

    public static bool IsValid(string? value) => value is Active or Paused or Archived;
}
