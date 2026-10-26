using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using Matching.Application.Common.Models;
using Matching.Domain.ValueObjects;
using Matching.WebApi.Mapping;

namespace Matching.Tests.Api;

/// <summary>Профіль AutoMapper: ObjectId перетворюється на рядок і назад, Value Objects — через конструктор.</summary>
public class MappingProfileTests
{
    private static IMapper CreateMapper()
        => new MapperConfiguration(cfg => cfg.AddProfile<MatchingProfile>(), NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public void ObjectId_maps_to_string_and_back()
    {
        var mapper = CreateMapper();
        var id = ObjectId.GenerateNewId();

        var text = mapper.Map<string>(id);

        Assert.Equal(id.ToString(), text);
        Assert.Equal(id, mapper.Map<ObjectId>(text));
    }

    [Fact]
    public void Language_item_maps_to_value_object_and_validates_in_constructor()
    {
        var mapper = CreateMapper();

        var vo = mapper.Map<Language>(new LanguageItem("UK", 4));

        Assert.Equal(new Language("uk", 4), vo);                                      // код нормалізується в конструкторі
        Assert.ThrowsAny<Exception>(() => mapper.Map<Language>(new LanguageItem("uk", 10)));
    }

    [Fact]
    public void Availability_slot_maps_to_item_and_back()
    {
        var mapper = CreateMapper();
        var slot = new AvailabilitySlot(2, "10:00", "12:00");

        var item = mapper.Map<AvailabilityItem>(slot);

        Assert.Equal(new AvailabilityItem(2, "10:00", "12:00"), item);
        Assert.Equal(slot, mapper.Map<AvailabilitySlot>(item));
    }
}
