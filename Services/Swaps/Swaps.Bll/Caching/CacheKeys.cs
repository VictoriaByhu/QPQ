namespace Swaps.Bll.Caching;

public static class CacheKeys
{
    public const string AllSkills = "swaps:skills:all";

    public static string Skill(int id) => $"swaps:skill:{id}";

    public static string Swap(int id) => $"swaps:swap:{id}";
}
