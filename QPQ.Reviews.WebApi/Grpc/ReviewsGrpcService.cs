using Grpc.Core;
using MediatR;
using QPQ.Reviews.Application.Reviews.Queries.GetReviewById;
using QPQ.Reviews.Grpc; // Це неймспейс, який ми задали в .proto файлі

namespace QPQ.Reviews.WebApi.Grpc;

// Успадковуємо базовий клас, який згенерувався автоматично
public class ReviewsGrpcService : ReviewsService.ReviewsServiceBase
{
    private readonly ISender _sender;
    private readonly ILogger<ReviewsGrpcService> _logger;

    public ReviewsGrpcService(ISender sender, ILogger<ReviewsGrpcService> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    // Перевизначаємо метод, описаний у .proto
    public override async Task<ReviewReply> GetReview(GetReviewRequest request, ServerCallContext context)
    {
        _logger.LogInformation("gRPC request received for Review ID: {ReviewId}", request.Id);

        // Викликаємо твою бізнес-логіку (CQRS)
        var review = await _sender.Send(new GetReviewByIdQuery(request.Id), context.CancellationToken);

        if (review == null)
        {
            // gRPC вимагає повертати помилки через RpcException зі статус-кодами
            throw new RpcException(new Status(StatusCode.NotFound, $"Review with ID {request.Id} not found"));
        }

        // Мапимо твій DTO у згенерований gRPC-тип відповіді
        return new ReviewReply
        {
            Id = review.Id,
            Text = review.Comment,
            Rating = review.Rating
        };
    }
}