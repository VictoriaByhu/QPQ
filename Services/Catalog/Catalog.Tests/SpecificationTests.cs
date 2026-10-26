using Catalog.Domain.Models;
using Catalog.Domain.Specifications;

namespace Catalog.Tests;

/// <summary>Специфікації перевіряються на звичайній колекції — без EF і бази.</summary>
public class SpecificationTests
{
    private static List<Skill> Sample()
    {
        var music = new Category("Музика", "music");
        var code = new Category("Програмування", "programming");
        Skill Make(Category c, int catId, string name, string slug)
        {
            var s = new Skill(catId, name, slug);
            typeof(Skill).GetProperty(nameof(Skill.Category))!.SetValue(s, c);
            return s;
        }
        var deleted = Make(code, 2, "Старе", "old");
        deleted.SoftDelete();
        return
        [
            Make(code, 2, "Програмування C#", "csharp"),
            Make(music, 1, "Гра на гітарі", "guitar"),
            Make(music, 1, "Гра на піаніно", "piano"),
            deleted
        ];
    }

    [Fact]
    public void Filter_ExcludesDeleted_AndSortsByName()
    {
        var result = new SkillsFilterSpec(new SkillFilter()).Evaluate(Sample()).ToList();
        Assert.Equal(["Гра на гітарі", "Гра на піаніно", "Програмування C#"], result.Select(s => s.Name));
    }

    [Fact]
    public void Filter_BySearchAndCategory()
    {
        var result = new SkillsFilterSpec(new SkillFilter(Search: "Гра", CategoryId: 1)).Evaluate(Sample()).ToList();
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Filter_PagingAppliesSkipTake_ButCountSpecDoesNot()
    {
        var page = new SkillsFilterSpec(new SkillFilter(Skip: 1, Take: 1)).Evaluate(Sample()).ToList();
        Assert.Single(page);
        Assert.Equal("Гра на піаніно", page[0].Name);

        var all = new SkillsFilterSpec(new SkillFilter(Skip: 1, Take: 1), paged: false).Evaluate(Sample()).ToList();
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public void Filter_DescendingSort()
    {
        var result = new SkillsFilterSpec(new SkillFilter(Descending: true)).Evaluate(Sample()).ToList();
        Assert.Equal("Програмування C#", result[0].Name);
    }

    [Fact]
    public void SecondSpec_ByCategory_WorksWithoutChangingRepository()
    {
        var result = new SkillsByCategorySpec(1).Evaluate(Sample()).ToList();
        Assert.Equal(["Гра на гітарі", "Гра на піаніно"], result.Select(s => s.Name));
    }
}
