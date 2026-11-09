using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using UserSkills.Bll.Dtos;
using UserSkills.Bll.Services;
using UserSkills.Domain.Exceptions;
using UserSkills.GrpcContracts;

namespace UserSkills.Api.Grpc;

/// <summary>
/// gRPC-адаптер над IUserSkillService (Лаб. 02): та сама бізнес-логіка, що й у REST-контролера.
/// Доменні винятки -> RpcException з відповідним StatusCode, а не загальний Internal.
/// </summary>
public sealed class UserSkillsGrpcService(IUserSkillService userSkills) : UserSkillsService.UserSkillsServiceBase
{
    private const int DefaultLimit = 20;

    public override async Task<UserSkillsReply> ListBySkill(ListBySkillRequest request, ServerCallContext context)
    {
        try
        {
            var limit = request.Limit == 0 ? DefaultLimit : request.Limit;
            var items = await userSkills.ListBySkillAsync(
                request.SkillId, request.HasType ? request.Type : null, limit, context.CancellationToken);

            var reply = new UserSkillsReply();
            reply.UserSkills.AddRange(items.Select(ToMessage));
            return reply;
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    private static UserSkillSummary ToMessage(UserSkillSummaryDto d) => new()
    {
        Id = d.Id, UserId = d.UserId.ToString(), SkillId = d.SkillId, SkillName = d.SkillName,
        Type = d.Type, Level = d.Level, Status = d.Status,
        // Timestamp вимагає UTC: з бази DateTime приходить з Kind=Unspecified
        CreatedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(d.CreatedAt, DateTimeKind.Utc))
    };
}
