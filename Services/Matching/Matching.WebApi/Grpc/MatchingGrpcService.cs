using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Matching.Application.Common.Exceptions;
using Matching.Application.SkillStats.Queries.GetSkillStats;
using Matching.GrpcContracts;
using MediatR;

namespace Matching.WebApi.Grpc;

/// <summary>
/// gRPC-адаптер над CQRS-запитами Лаб. 04: надсилає той самий GetSkillStatsQuery через MediatR
/// (валідація, логування й вимірювання часу — у pipeline behaviors), що й REST-контролер.
/// NotFound -> NotFound, помилки валідації -> InvalidArgument.
/// </summary>
public sealed class MatchingGrpcService(ISender mediator) : MatchingService.MatchingServiceBase
{
    public override async Task<SkillStatsReply> GetSkillStats(GetSkillStatsRequest request, ServerCallContext context)
    {
        try
        {
            var stats = await mediator.Send(new GetSkillStatsQuery(request.SkillId), context.CancellationToken);

            var reply = new SkillStatsReply
            {
                SkillId = stats.SkillId, SkillName = stats.SkillName,
                OffersCount = stats.OffersCount, WantsCount = stats.WantsCount,
                UpdatedAt = Timestamp.FromDateTime(stats.UpdatedAt.Kind == DateTimeKind.Utc
                    ? stats.UpdatedAt
                    : DateTime.SpecifyKind(stats.UpdatedAt, DateTimeKind.Utc))
            };
            reply.TopLanguages.AddRange(stats.TopLanguages.Select(l => new LanguageCount { Code = l.Code, Count = l.Count }));
            return reply;
        }
        catch (NotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ValidationException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }
}
