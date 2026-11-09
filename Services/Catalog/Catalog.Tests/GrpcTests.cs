using System.Net.Http.Json;
using System.Text.Json;
using Catalog.Bll.Dtos;
using Catalog.GrpcContracts;
using Grpc.Core;
using Grpc.Net.Client;

namespace Catalog.Tests;

/// <summary>
/// gRPC-контракт CatalogService: справжній конвеєр API на InMemory-базі, виклики через TestServer-handler.
/// Перевіряє перетворення моделей у повідомлення й доменних винятків у RpcException зі статусом.
/// </summary>
public sealed class GrpcTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly GrpcChannel _channel;
    private readonly CatalogService.CatalogServiceClient _client;

    public GrpcTests()
    {
        _factory.CreateClient();                                              // запускає тестовий сервер
        _channel = GrpcChannel.ForAddress("http://localhost",
            new GrpcChannelOptions { HttpHandler = _factory.Server.CreateHandler() });
        _client = new CatalogService.CatalogServiceClient(_channel);
    }

    public void Dispose()
    {
        _channel.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task GetSkill_ReturnsMappedMessage()
    {
        var reply = await _client.GetSkillAsync(new GetSkillRequest { Id = 1 });

        Assert.Equal(1, reply.Id);
        Assert.Equal("csharp", reply.Slug);
        Assert.Equal("Програмування", reply.CategoryName);
        Assert.Equal(5, reply.Levels.Count);
        Assert.NotEmpty(reply.Tags);
    }

    [Fact]
    public async Task GetSkill_UnknownId_ReturnsNotFound_NotInternal()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() => _client.GetSkillAsync(new GetSkillRequest { Id = 99999 }).ResponseAsync);
        Assert.Equal(StatusCode.NotFound, ex.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task GetSkill_NonPositiveId_ReturnsInvalidArgument(int id)
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() => _client.GetSkillAsync(new GetSkillRequest { Id = id }).ResponseAsync);
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task GetSkillsByIds_SkipsMissingIds()
    {
        var request = new GetSkillsByIdsRequest();
        request.Ids.AddRange([1, 2, 99999]);

        var reply = await _client.GetSkillsByIdsAsync(request);

        Assert.Equal([1, 2], reply.Skills.Select(s => s.Id).OrderBy(i => i));
    }

    [Fact]
    public async Task GetSkillsByIds_EmptyList_ReturnsInvalidArgument()
    {
        var ex = await Assert.ThrowsAsync<RpcException>(() => _client.GetSkillsByIdsAsync(new GetSkillsByIdsRequest()).ResponseAsync);
        Assert.Equal(StatusCode.InvalidArgument, ex.StatusCode);
    }

    [Fact]
    public async Task GrpcAndRest_ShareTheSameCache_AndSeeUpdates()
    {
        using var http = _factory.CreateClient();
        var before = await _client.GetSkillAsync(new GetSkillRequest { Id = 1 });          // заповнює кеш
        var rest = await http.GetFromJsonAsync<SkillDto>("/api/skills/1", Json);

        var put = await http.PutAsJsonAsync("/api/skills/1", new
        {
            categoryId = before.CategoryId, name = "C# (через REST)", slug = before.Slug,
            description = before.Description, iconUrl = before.IconUrl, rowVersion = rest!.RowVersion
        });
        put.EnsureSuccessStatusCode();

        var after = await _client.GetSkillAsync(new GetSkillRequest { Id = 1 });           // gRPC бачить зміну з REST
        Assert.Equal("C# (через REST)", after.Name);
    }

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
}
