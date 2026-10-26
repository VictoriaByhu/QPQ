using System.ComponentModel.DataAnnotations;
using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Bll.Mapping;
using Catalog.Bll.Validators;
using Catalog.Domain.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace Catalog.Tests;

public class MappingAndValidationTests
{
    internal static IMapper CreateMapper()
        => new MapperConfiguration(c => c.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void MappingConfiguration_IsValid()
        => new MapperConfiguration(c => c.AddProfile<CatalogProfile>(), NullLoggerFactory.Instance)
            .AssertConfigurationIsValid();

    [Fact]
    public void CreateSkillDto_EmptyName_FailsDataAnnotations()
    {
        var dto = new CreateSkillDto { Name = "", Slug = "csharp", CategoryId = 1 };
        var errors = new List<ValidationResult>();
        Assert.False(Validator.TryValidateObject(dto, new ValidationContext(dto), errors, validateAllProperties: true));
        Assert.Contains(errors, e => e.MemberNames.Contains("Name"));
    }

    [Theory]
    [InlineData("csharp", true)]
    [InlineData("english-speaking", true)]
    [InlineData("Bad Slug", false)]
    [InlineData("double--dash", false)]
    [InlineData("UPPER", false)]
    public void Slug_Rule(string slug, bool valid)
    {
        var result = new CreateSkillDtoValidator().Validate(new CreateSkillDto { Name = "x", Slug = slug, CategoryId = 1 });
        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void IconUrl_MustBeAbsoluteHttp()
    {
        var validator = new CreateSkillDtoValidator();
        Assert.True(validator.Validate(new CreateSkillDto { Name = "x", Slug = "x", CategoryId = 1, IconUrl = "https://a/b.svg" }).IsValid);
        Assert.False(validator.Validate(new CreateSkillDto { Name = "x", Slug = "x", CategoryId = 1, IconUrl = "javascript:alert(1)" }).IsValid);
    }

    [Fact]
    public void TagIds_MustBeDistinct()
    {
        var result = new CreateSkillDtoValidator()
            .Validate(new CreateSkillDto { Name = "x", Slug = "x", CategoryId = 1, TagIds = [1, 1] });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void SkillDto_MapsFlattenedAndSortedParts()
    {
        var skill = new Skill(1, "C#", "csharp");
        skill.SetDetail("desc", null);
        skill.AddDefaultLevels();
        typeof(Skill).GetProperty(nameof(Skill.Category))!.SetValue(skill, new Category("Програмування", "programming"));

        var dto = CreateMapper().Map<SkillDto>(skill);

        Assert.Equal("Програмування", dto.CategoryName);
        Assert.Equal("desc", dto.Description);
        Assert.Equal(5, dto.Levels.Count);
        Assert.Equal((byte)1, dto.Levels[0].Level);
    }
}
