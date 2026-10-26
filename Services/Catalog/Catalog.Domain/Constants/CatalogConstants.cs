using System.Text.RegularExpressions;

namespace Catalog.Domain.Constants;

public static class CatalogConstants
{
    public const int NameMaxLength = 100;
    public const int SlugMaxLength = 100;
    public const int TagNameMaxLength = 50;
    public const int DescriptionMaxLength = 2000;
    public const int IconUrlMaxLength = 500;
    public const int LevelTitleMaxLength = 100;
    public const int LevelDescriptionMaxLength = 500;
    public const byte MinLevel = 1;
    public const byte MaxLevel = 5;

    /// <summary>Slug: малі латинські літери, цифри й дефіси, без пробілів; збігається з CHECK у базі.</summary>
    public const string SlugPattern = "^[a-z0-9]+(?:-[a-z0-9]+)*$";

    public static readonly Regex SlugRegex = new(SlugPattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Шкала рівнів за замовчуванням: на неї посилається UserSkills.Level.</summary>
    public static readonly string[] DefaultLevelTitles =
        ["Початківець", "Базовий", "Середній", "Просунутий", "Експерт"];
}
