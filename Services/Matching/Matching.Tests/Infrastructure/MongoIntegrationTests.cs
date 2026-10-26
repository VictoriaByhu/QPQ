using MongoDB.Bson;
using MongoDB.Driver;
using Matching.Application.Common.Models;
using Matching.Domain.Entities;
using Matching.Domain.Enums;
using Matching.Domain.Exceptions;
using Matching.Infrastructure.Persistence;
using Matching.Infrastructure.Persistence.Queries;
using Matching.Infrastructure.Persistence.Repositories;
using Matching.Infrastructure.Seeding;
using static Matching.Tests.TestData;

namespace Matching.Tests.Infrastructure;

/// <summary>Інтеграційні тести на справжньому mongod (Mongo2Go): репозиторії, індекси, запити, сідінг.</summary>
[Collection(MongoCollection.Name)]
public class MongoIntegrationTests(MongoFixture mongo)
{
    private async Task<MongoDbContext> ContextWithIndexesAsync()
    {
        var context = mongo.NewContext();
        await IndexCreationService.CreateIndexesAsync(context);
        return context;
    }

    [MongoFact]
    public async Task Repository_stores_and_reads_back_an_entry()
    {
        var context = await ContextWithIndexesAsync();
        var repo = new UserSkillIndexRepository(context);
        var entry = Entry(languages: ["uk", "en"]);

        await repo.AddAsync(entry);
        var loaded = await repo.GetByIdAsync(entry.Id);

        Assert.NotNull(loaded);
        Assert.Equal(entry.Languages, loaded!.Languages);
        Assert.True(await repo.ExistsByUserSkillIdAsync(1));
        Assert.False(await repo.ExistsByUserSkillIdAsync(99));
    }

    [MongoFact]
    public async Task Raw_document_has_the_structure_from_lab_01_without_dotnet_internals()
    {
        var context = await ContextWithIndexesAsync();
        await new UserSkillIndexRepository(context).AddAsync(Entry());

        var raw = await context.Raw(CollectionNames.UserSkillIndex).Find(FilterDefinition<BsonDocument>.Empty).FirstAsync();

        Assert.True(raw.Contains("userSkillId") && raw.Contains("languages") && raw.Contains("availability"));
        Assert.DoesNotContain(raw.Names, n => char.IsUpper(n[0]) || (n.StartsWith('_') && n != "_id"));
    }

    [MongoFact]
    public async Task Concurrent_update_with_a_stale_version_is_rejected()
    {
        var context = await ContextWithIndexesAsync();
        var repo = new UserSkillIndexRepository(context);
        var entry = Entry();
        await repo.AddAsync(entry);

        var first = (await repo.GetByIdAsync(entry.Id))!;
        var second = (await repo.GetByIdAsync(entry.Id))!;

        first.Update("First", 3, UserSkillStatus.Active, 1, [], []);
        await repo.UpdateAsync(first);                                           // версія 0 -> 1

        second.Update("Second", 2, UserSkillStatus.Active, 1, [], []);
        await Assert.ThrowsAsync<ConflictException>(() => repo.UpdateAsync(second));

        var stored = (await repo.GetByIdAsync(entry.Id))!;
        Assert.Equal("First", stored.SkillName);
        Assert.Equal(1, stored.Version);
    }

    [MongoFact]
    public async Task Legacy_document_without_version_can_still_be_updated()
    {
        var context = await ContextWithIndexesAsync();
        var repo = new UserSkillIndexRepository(context);
        var entry = Entry();
        await repo.AddAsync(entry);
        await context.Raw(CollectionNames.UserSkillIndex)
            .UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", entry.Id), Builders<BsonDocument>.Update.Unset("version"));

        var legacy = (await repo.GetByIdAsync(entry.Id))!;
        legacy.Update("Migrated", 3, UserSkillStatus.Active, 1, [], []);
        await repo.UpdateAsync(legacy);

        Assert.Equal(1, (await repo.GetByIdAsync(entry.Id))!.Version);
    }

    [MongoFact]
    public async Task Duplicate_user_skill_id_is_rejected_by_the_unique_index()
    {
        var context = await ContextWithIndexesAsync();
        var repo = new UserSkillIndexRepository(context);
        await repo.AddAsync(Entry(userSkillId: 7));

        var ex = await Assert.ThrowsAsync<MongoWriteException>(() => repo.AddAsync(Entry(userSkillId: 7)));
        Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category);
    }

    [MongoFact]
    public async Task Duplicate_match_offer_is_rejected_by_the_unique_index()
    {
        var context = await ContextWithIndexesAsync();
        var repo = new MatchRepository(context);
        await repo.AddAsync(Match.OneWay(Offer(U1, U2), 70, ["uk"]));

        await Assert.ThrowsAsync<MongoWriteException>(() => repo.AddAsync(Match.OneWay(Offer(U1, U2), 60, ["uk"])));
        Assert.NotNull(await repo.GetByOfferKeyAsync(U1, U2, 1));
        Assert.Null(await repo.GetByOfferKeyAsync(U2, U1, 1));
    }

    [MongoFact]
    public async Task Indexes_from_the_lab_are_created_and_creation_is_repeatable()
    {
        var context = await ContextWithIndexesAsync();
        await IndexCreationService.CreateIndexesAsync(context);                  // повторно: без помилок і дублів

        async Task<List<string>> Names(string collection)
            => (await (await context.Raw(collection).Indexes.ListAsync()).ToListAsync())
                .Select(i => i["name"].AsString).ToList();

        var index = await Names(CollectionNames.UserSkillIndex);
        var matches = await Names(CollectionNames.Matches);

        Assert.Contains("ux_userSkillIndex_userSkillId", index);
        Assert.Contains("ix_userSkillIndex_skill_type_active", index);
        Assert.Contains("ix_userSkillIndex_user_status", index);
        Assert.Contains("tx_userSkillIndex_skillName", index);
        Assert.Contains("ux_matches_offer", matches);
        Assert.Contains("ix_matches_user_status_score", matches);
        Assert.Contains("ttl_matches_expiresAt", matches);
        Assert.Equal(5, index.Count);                                            // _id + 4
        Assert.Equal(4, matches.Count);
    }

    [MongoFact]
    public async Task Seeding_is_idempotent()
    {
        var context = await ContextWithIndexesAsync();
        var stats = new SkillStatsRepository(context);
        IDataSeeder[] seeders = [new UserSkillIndexSeeder(context), new MatchSeeder(context), new SkillStatsSeeder(context, stats)];

        foreach (var s in seeders) await s.SeedAsync();
        var first = (await context.UserSkillIndex.CountDocumentsAsync(FilterDefinition<UserSkillIndexEntry>.Empty),
                     await context.Matches.CountDocumentsAsync(FilterDefinition<Match>.Empty),
                     await context.SkillStats.CountDocumentsAsync(FilterDefinition<SkillStats>.Empty));

        foreach (var s in seeders) await s.SeedAsync();
        var second = (await context.UserSkillIndex.CountDocumentsAsync(FilterDefinition<UserSkillIndexEntry>.Empty),
                      await context.Matches.CountDocumentsAsync(FilterDefinition<Match>.Empty),
                      await context.SkillStats.CountDocumentsAsync(FilterDefinition<SkillStats>.Empty));

        Assert.Equal((10L, 3L, 5L), first);                                      // 5 навичок мають активні записи
        Assert.Equal(first, second);
    }

    [MongoFact]
    public async Task Seeded_documents_pass_through_domain_validation_when_read()
    {
        var context = await ContextWithIndexesAsync();
        await new UserSkillIndexSeeder(context).SeedAsync();
        await new MatchSeeder(context).SeedAsync();

        var entries = await context.UserSkillIndex.Find(FilterDefinition<UserSkillIndexEntry>.Empty).ToListAsync();
        var matches = await context.Matches.Find(FilterDefinition<Match>.Empty).ToListAsync();

        Assert.Equal(10, entries.Count);
        Assert.Contains(matches, m => m.Kind == MatchKind.Mutual && m.CounterOffer is not null);
    }

    [MongoFact]
    public async Task Queries_filter_and_paginate_inside_mongodb()
    {
        var context = await ContextWithIndexesAsync();
        await new UserSkillIndexSeeder(context).SeedAsync();
        var queries = new UserSkillIndexQueries(context);

        var offers = await queries.GetListAsync(null, 1, "Offer", 1, 20, default);
        Assert.Equal(1, offers.TotalCount);
        Assert.Equal("Offer", offers.Items[0].Type);

        var page = await queries.GetListAsync(null, null, null, 2, 4, default);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.Equal(4, page.Items.Count);

        var one = await queries.GetByIdAsync("670000000000000000000d01", default);
        Assert.Equal("Програмування C#", one!.SkillName);
        Assert.Equal(2, one.Languages.Count);
        Assert.Null(await queries.GetByIdAsync("not-an-object-id", default));
    }

    [MongoFact]
    public async Task Match_query_returns_only_the_users_pairs_hides_dismissed_and_sorts_by_score()
    {
        var context = await ContextWithIndexesAsync();
        await new MatchSeeder(context).SeedAsync();
        var queries = new MatchQueries(context);

        var u1 = await queries.GetForUserAsync(U1, includeDismissed: false, 1, 20, default);
        Assert.Single(u1.Items);                                                  // e03 відхилена
        Assert.Equal("mutual", u1.Items[0].Kind);

        var all = await queries.GetForUserAsync(U1, includeDismissed: true, 1, 20, default);
        Assert.Equal([95, 55], all.Items.Select(m => m.Score));

        var u2 = await queries.GetForUserAsync(U2, false, 1, 20, default);
        Assert.Equal(2, u2.TotalCount);
        Assert.All(u2.Items, m => Assert.Contains(U2, m.UserIds));
    }

    [MongoFact]
    public async Task Skill_stats_are_rebuilt_by_aggregation()
    {
        var context = await ContextWithIndexesAsync();
        await new UserSkillIndexSeeder(context).SeedAsync();
        var repo = new SkillStatsRepository(context);

        Assert.Equal(5, await repo.RebuildAsync());
        Assert.Equal(5, await repo.RebuildAsync());                               // повторно: той самий результат

        var stats = new SkillStatsQueries(context);
        var csharp = (await stats.GetByIdAsync(1, default))!;
        Assert.Equal(1, csharp.OffersCount);
        Assert.Equal(1, csharp.WantsCount);                                       // Paused і Archived не рахуються
        Assert.Equal("uk", csharp.TopLanguages[0].Code);
        Assert.Equal(2, csharp.TopLanguages[0].Count);

        // Після видалення всіх активних записів навичка зникає зі статистики.
        await context.UserSkillIndex.DeleteManyAsync(x => x.SkillId == 5);
        Assert.Equal(4, await repo.RebuildAsync());
        Assert.Null(await stats.GetByIdAsync(5, default));
    }

    [MongoFact]
    public async Task Schema_validation_script_documents_match_what_the_app_writes()
    {
        // Колекція зі схемою з db/p3 (additionalProperties: false) приймає документ, який пише застосунок.
        var context = mongo.NewContext();
        var db = context.Database;
        await db.CreateCollectionAsync(CollectionNames.UserSkillIndex, new CreateCollectionOptions<BsonDocument>
        {
            Validator = new BsonDocumentFilterDefinition<BsonDocument>(BsonDocument.Parse(@"{ $jsonSchema: {
                bsonType: 'object',
                required: ['userSkillId','userId','skillId','skillName','type','level','status','languages','availability','updatedAt'],
                additionalProperties: false,
                properties: {
                    _id: { bsonType: 'objectId' }, userSkillId: { bsonType: 'number' }, userId: { bsonType: 'string' },
                    skillId: { bsonType: 'number' }, skillName: { bsonType: 'string' }, type: { enum: ['Offer','Want'] },
                    level: { bsonType: 'number' }, status: { enum: ['Active','Paused','Archived'] },
                    experienceYears: { bsonType: 'number' }, languages: { bsonType: 'array' }, availability: { bsonType: 'array' },
                    updatedAt: { bsonType: 'date' }, createdAt: { bsonType: 'date' }, version: { bsonType: 'number' } } } }")),
            ValidationAction = DocumentValidationAction.Error,
            ValidationLevel = DocumentValidationLevel.Strict
        });

        await new UserSkillIndexRepository(context).AddAsync(Entry());           // не кидає MongoWriteException
        Assert.Equal(1, await context.UserSkillIndex.CountDocumentsAsync(FilterDefinition<UserSkillIndexEntry>.Empty));
    }
}
