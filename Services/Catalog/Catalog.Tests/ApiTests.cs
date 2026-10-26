using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Bll.Dtos;
using Catalog.Dal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Catalog.Tests;

/// <summary>Кожен тест отримує власний екземпляр API й чисту InMemory-базу з початковими даними.</summary>
public sealed class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient _client;

    public ApiTests() => _client = factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        factory.Dispose();
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetList_ReturnsSeededSkills_WithPagingMetadata()
    {
        var response = await _client.GetAsync("/api/skills?page=2&pageSize=4");
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SkillListItemDto>>(Json);

        Assert.Equal(6, page!.TotalCount);          // усі записи, а не лише сторінка
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
        Assert.Equal("6", response.Headers.GetValues("X-Total-Count").Single());
    }

    [Fact]
    public async Task GetList_FiltersByCategoryAndSorts()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<SkillListItemDto>>(
            "/api/skills?categoryId=1&sortBy=name&sortDir=desc", Json);
        Assert.Single(page!.Items);
        Assert.Equal("Програмування C#", page.Items[0].Name);
    }

    [Fact]
    public async Task GetList_FiltersByTag()
    {
        var page = await _client.GetFromJsonAsync<PagedResult<SkillListItemDto>>("/api/skills?tag=.NET", Json);
        Assert.Equal(1, page!.TotalCount);
    }

    [Fact]
    public async Task GetList_InvalidSortBy_Is400()
    {
        var response = await _client.GetAsync("/api/skills?sortBy=hacker");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_Eager_ReturnsDetailLevelsAndTags()
    {
        var skill = await _client.GetFromJsonAsync<SkillDto>("/api/skills/1", Json);
        Assert.Equal("Програмування", skill!.CategoryName);
        Assert.NotNull(skill.Description);
        Assert.Equal(5, skill.Levels.Count);
        Assert.Contains(".NET", skill.Tags);
    }

    [Fact]
    public async Task GetById_Unknown_Is404ProblemDetails()
    {
        var response = await _client.GetAsync("/api/skills/9999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Create_EmptyName_Is400WithFieldErrors()
    {
        var response = await _client.PostAsJsonAsync("/api/skills", new { name = "", slug = "x", categoryId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"errors\"", body);
        Assert.Contains("Name", body);
    }

    [Fact]
    public async Task Create_BadSlug_Is400FromFluentValidation()
    {
        var response = await _client.PostAsJsonAsync("/api/skills", new { name = "Йога", slug = "Bad Slug", categoryId = 1 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Slug", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Create_Returns201WithLocation_AndDuplicateIs409()
    {
        var input = new { name = "Йога", slug = "yoga", categoryId = 1, description = "Асани", tagIds = new[] { 2 } };
        var created = await _client.PostAsJsonAsync("/api/skills", input);

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var dto = await created.Content.ReadFromJsonAsync<SkillDto>(Json);
        Assert.Equal(5, dto!.Levels.Count);

        var get = await _client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var duplicate = await _client.PostAsJsonAsync("/api/skills", input);
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownCategory_Is400()
    {
        var response = await _client.PostAsJsonAsync("/api/skills", new { name = "Х", slug = "x-skill", categoryId = 999 });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_SoftDeletes_ThenGetIs404()
    {
        var created = await _client.PostAsJsonAsync("/api/skills", new { name = "Тимчасова", slug = "temp", categoryId = 1 });
        var dto = await created.Content.ReadFromJsonAsync<SkillDto>(Json);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/skills/{dto!.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/skills/{dto.Id}")).StatusCode);
    }

    [Fact]
    public async Task AddTag_Twice_Is409()
    {
        var first = await _client.PostAsync("/api/skills/4/tags/6", null);
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        var second = await _client.PostAsync("/api/skills/4/tags/6", null);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Category_Explicit_ReturnsItsSkills()
    {
        var category = await _client.GetFromJsonAsync<CategoryWithSkillsDto>("/api/categories/2", Json);
        Assert.Single(category!.Skills);
        Assert.Equal("Англійська розмовна", category.Skills[0].Name);
    }

    [Fact]
    public async Task Category_WithActiveSkills_CannotBeDeleted()
    {
        var response = await _client.DeleteAsync("/api/categories/1");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Category_CreateDuplicateName_Is409()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new { name = "Музика", slug = "music-2", sortOrder = 5 });
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ByTag_UsesIntermediateEntity()
    {
        var skills = await _client.GetFromJsonAsync<List<SkillBriefDto>>("/api/skills/by-tag?tagName=Мови", Json);
        Assert.Single(skills!);
        Assert.Equal("Англійська розмовна", skills![0].Name);
    }

    [Fact]
    public async Task UpdateLevel_ChangesTitle()
    {
        var response = await _client.PutAsJsonAsync("/api/skills/1/levels/5", new { title = "Гуру" });
        response.EnsureSuccessStatusCode();
        var level = await response.Content.ReadFromJsonAsync<SkillLevelDto>(Json);
        Assert.Equal("Гуру", level!.Title);
    }

    [Fact]
    public async Task Seeding_IsIdempotent()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        await DbInitializer.SeedAsync(db);
        var levels = await db.SkillLevels.CountAsync();
        var links = await db.SkillTags.CountAsync();
        var details = await db.SkillDetails.CountAsync();

        await DbInitializer.SeedAsync(db);
        await DbInitializer.SeedAsync(db);

        Assert.Equal(levels, await db.SkillLevels.CountAsync());
        Assert.Equal(links, await db.SkillTags.CountAsync());
        Assert.Equal(details, await db.SkillDetails.CountAsync());
        Assert.Equal(6 * 5, levels);
    }
}
