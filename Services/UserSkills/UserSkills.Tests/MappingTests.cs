using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using UserSkills.Bll.Mapping;

namespace UserSkills.Tests;

public class MappingTests
{
    internal static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserSkillProfile>(),
            NullLoggerFactory.Instance);
        return config.CreateMapper();
    }

    [Fact]
    public void MappingConfiguration_IsValid()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UserSkillProfile>(),
            NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();
    }
}
