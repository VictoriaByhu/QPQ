using Catalog.Domain.Exceptions;
using Catalog.Domain.Models;

namespace Catalog.Tests;

public class DomainTests
{
    [Fact]
    public void Category_RejectsBadSlug()
        => Assert.Throws<DomainException>(() => new Category("Музика", "Bad Slug"));

    [Fact]
    public void Category_RejectsEmptyName()
        => Assert.Throws<DomainException>(() => new Category("  ", "music"));

    [Fact]
    public void Skill_AddDefaultLevels_CreatesFiveLevelsOnce()
    {
        var skill = new Skill(1, "C#", "csharp");
        skill.AddDefaultLevels();
        skill.AddDefaultLevels();
        Assert.Equal([1, 2, 3, 4, 5], skill.Levels.Select(l => (int)l.Level).Order());
    }

    [Fact]
    public void Skill_AddTag_Twice_Throws()
    {
        var skill = new Skill(1, "C#", "csharp");
        skill.AddTag(1);
        Assert.Throws<DomainException>(() => skill.AddTag(1));
    }

    [Fact]
    public void Skill_RemoveUnknownTag_IsNotFound()
    {
        var skill = new Skill(1, "C#", "csharp");
        Assert.Throws<EntityNotFoundException>(() => skill.RemoveTag(7));
    }

    [Fact]
    public void Skill_SetDetail_UpdatesExisting()
    {
        var skill = new Skill(1, "C#", "csharp");
        skill.SetDetail("a", null);
        var first = skill.Detail;
        skill.SetDetail("b", "https://x/y.svg");
        Assert.Same(first, skill.Detail);
        Assert.Equal("b", skill.Detail!.Description);
    }

    [Fact]
    public void Skill_SoftDelete_BlocksFurtherChanges()
    {
        var skill = new Skill(1, "C#", "csharp");
        skill.SoftDelete();
        Assert.True(skill.IsDeleted);
        Assert.Throws<DomainException>(() => skill.Update(1, "C# 2", "csharp-2"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void SkillLevel_RejectsOutOfRange(byte level)
        => Assert.Throws<DomainException>(() => new SkillLevel(level, "x", null));
}
