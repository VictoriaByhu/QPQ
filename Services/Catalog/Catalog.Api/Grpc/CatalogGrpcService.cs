using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Catalog.Domain.Exceptions;
using Catalog.GrpcContracts;
using Grpc.Core;

namespace Catalog.Api.Grpc;

/// <summary>
/// gRPC-адаптер над бізнес-логікою Лаб. 03: так само, як контролер, лише перетворює повідомлення й помилки.
/// ISkillService тут — декоратор із кешем, тож gRPC і REST діляться одним L1/L2.
/// Доменні винятки перетворюються на RpcException із відповідним StatusCode (не загальний Internal).
/// </summary>
public sealed class CatalogGrpcService(ISkillService skills) : CatalogService.CatalogServiceBase
{
    private const int MaxIds = 100;

    public override async Task<SkillReply> GetSkill(GetSkillRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

        try
        {
            return ToReply(await skills.GetByIdAsync(request.Id, context.CancellationToken));
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<SkillsReply> GetSkillsByIds(GetSkillsByIdsRequest request, ServerCallContext context)
    {
        if (request.Ids.Count is 0 or > MaxIds)
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Provide from 1 to {MaxIds} ids"));
        if (request.Ids.Any(id => id <= 0))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Ids must be positive"));

        var reply = new SkillsReply();
        foreach (var id in request.Ids.Distinct())
        {
            try
            {
                reply.Skills.Add(ToReply(await skills.GetByIdAsync(id, context.CancellationToken)));
            }
            catch (EntityNotFoundException)
            {
                // відсутня навичка — не помилка пакетного запиту: клієнт побачить її відсутність у відповіді
            }
        }
        return reply;
    }

    private static SkillReply ToReply(SkillDto s)
    {
        var reply = new SkillReply
        {
            Id = s.Id, Name = s.Name, Slug = s.Slug, CategoryId = s.CategoryId, CategoryName = s.CategoryName
        };
        if (s.Description is not null) reply.Description = s.Description;
        if (s.IconUrl is not null) reply.IconUrl = s.IconUrl;
        reply.Tags.AddRange(s.Tags);
        reply.Levels.AddRange(s.Levels.Select(l =>
        {
            var level = new SkillLevel { Level = l.Level, Title = l.Title };
            if (l.Description is not null) level.Description = l.Description;
            return level;
        }));
        return reply;
    }
}
