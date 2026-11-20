using MediatR;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Domain.Entities;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _repository;
    private readonly IReviewsMetrics _metrics;

    public CreateReviewCommandHandler(IReviewRepository repository, IReviewsMetrics metrics)
    {
        _repository = repository;
        _metrics = metrics;
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _metrics.IncrementActiveOperations();

        try
        {
            var review = new Review(request.ProductId, request.Author, request.Rating, request.Comment);

            await _repository.AddAsync(review);

            _metrics.ReviewCreated(request.Rating);
            _metrics.RecordDuration("create", success: true, sw.Elapsed.TotalMilliseconds);

            return review.Id.ToString();
        }
        catch
        {
            _metrics.RecordDuration("create", success: false, sw.Elapsed.TotalMilliseconds);
            throw;
        }
        finally
        {
            _metrics.DecrementActiveOperations();
        }
    }
}