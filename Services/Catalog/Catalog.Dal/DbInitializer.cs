using Microsoft.EntityFrameworkCore;

namespace Catalog.Dal;

/// <summary>
/// Програмний сідінг після MigrateAsync. Ідемпотентний: заповнює лише те, чого ще немає.
/// Категорії, навички й теги вже додані статично (HasData), тут — залежні записи.
/// </summary>
public static class DbInitializer
{
    // Теги навичок 1..6 (ідентифікатори тегів — зі статичного сідінгу).
    private static readonly Dictionary<int, int[]> TagsBySkill = new()
    {
        [1] = [1, 2, 3], [2] = [6, 3, 4], [3] = [5, 4, 2], [4] = [5, 3], [5] = [5, 2], [6] = [5, 4]
    };

    private static readonly Dictionary<int, (string Description, string IconUrl)> Details = new()
    {
        [1] = ("Основи C#, платформа .NET, ООП, робота з даними та веб-API", "https://cdn.example.com/icons/csharp.svg"),
        [2] = ("Розмовна англійська: діалоги, вимова, підготовка до співбесід", "https://cdn.example.com/icons/english.svg"),
        [3] = ("Акустична гітара: акорди, бій, перші пісні", "https://cdn.example.com/icons/guitar.svg"),
        [4] = ("Проєктування інтерфейсів: макети, компоненти, прототипи у Figma", "https://cdn.example.com/icons/ui.svg"),
        [5] = ("Знімання, композиція, світло та базова обробка фото", "https://cdn.example.com/icons/photo.svg"),
        [6] = ("Тісто, креми, випікання: від печива до пирогів", "https://cdn.example.com/icons/baking.svg")
    };

    public static async Task SeedAsync(CatalogDbContext db, CancellationToken ct = default)
    {
        var skills = await db.Skills
            .Include(s => s.Detail)
            .Include(s => s.Levels)
            .Include(s => s.SkillTags)
            .Where(s => s.Id <= 6)
            .ToListAsync(ct);

        foreach (var skill in skills)
        {
            if (skill.Detail is null && Details.TryGetValue(skill.Id, out var d))
                skill.SetDetail(d.Description, d.IconUrl);

            if (skill.Levels.Count == 0)
                skill.AddDefaultLevels();

            if (TagsBySkill.TryGetValue(skill.Id, out var tagIds))
                foreach (var tagId in tagIds.Where(t => skill.SkillTags.All(st => st.TagId != t)))
                    skill.AddTag(tagId);
        }

        await db.SaveChangesAsync(ct);
    }
}
