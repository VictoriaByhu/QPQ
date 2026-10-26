using AutoMapper;
using MongoDB.Bson;
using Matching.Domain.ValueObjects;
using Matching.Application.Common.Models;
using Matching.Application.IndexEntries.Commands.CreateIndexEntry;
using Matching.Application.IndexEntries.Commands.UpdateIndexEntry;
using Matching.Application.Matches.Commands.GenerateMatches;
using Matching.Application.Matches.Commands.UpdateMatchStatus;
using Matching.WebApi.Contracts;

namespace Matching.WebApi.Mapping;

/// <summary>Мапінг HTTP-контрактів у команди MediatR (вкладені масиви перетворюються на Application-моделі).</summary>
public sealed class MatchingProfile : Profile
{
    public MatchingProfile()
    {
        // ObjectId <-> string: у HTTP-контрактах ідентифікатори завжди рядки.
        CreateMap<ObjectId, string>().ConvertUsing(id => id.ToString());
        CreateMap<string, ObjectId>().ConvertUsing(s => ObjectId.Parse(s));

        // Value Objects домену <-> моделі Application (VO створюються через конструктор, тож інваріанти діють).
        CreateMap<LanguageItem, Language>().ConvertUsing(s => new Language(s.Code, s.Proficiency));
        CreateMap<Language, LanguageItem>().ConvertUsing(s => new LanguageItem(s.Code, s.Proficiency));
        CreateMap<AvailabilityItem, AvailabilitySlot>().ConvertUsing(s => new AvailabilitySlot(s.DayOfWeek, s.Start, s.End));
        CreateMap<AvailabilitySlot, AvailabilityItem>().ConvertUsing(s => new AvailabilityItem(s.DayOfWeek, s.Start, s.End));

        CreateMap<LanguageRequest, LanguageItem>();
        CreateMap<AvailabilityRequest, AvailabilityItem>();

        CreateMap<CreateIndexEntryRequest, CreateIndexEntryCommand>()
            .ForCtorParam(nameof(CreateIndexEntryCommand.Languages), o => o.MapFrom(s => s.Languages ?? new()))
            .ForCtorParam(nameof(CreateIndexEntryCommand.Availability), o => o.MapFrom(s => s.Availability ?? new()));

        // Id та ExpectedVersion надходять з маршруту й заголовка If-Match, тому в контракті їх немає.
        CreateMap<UpdateIndexEntryRequest, UpdateIndexEntryCommand>()
            .ForCtorParam(nameof(UpdateIndexEntryCommand.Id), o => o.MapFrom(_ => ""))
            .ForCtorParam(nameof(UpdateIndexEntryCommand.ExpectedVersion), o => o.MapFrom(_ => 0))
            .ForCtorParam(nameof(UpdateIndexEntryCommand.Languages), o => o.MapFrom(s => s.Languages ?? new()))
            .ForCtorParam(nameof(UpdateIndexEntryCommand.Availability), o => o.MapFrom(s => s.Availability ?? new()));

        CreateMap<UpdateMatchStatusRequest, UpdateMatchStatusCommand>()
            .ForCtorParam(nameof(UpdateMatchStatusCommand.Id), o => o.MapFrom(_ => ""))
            .ForCtorParam(nameof(UpdateMatchStatusCommand.ExpectedVersion), o => o.MapFrom(_ => 0));

        CreateMap<GenerateMatchesRequest, GenerateMatchesCommand>();
    }
}
