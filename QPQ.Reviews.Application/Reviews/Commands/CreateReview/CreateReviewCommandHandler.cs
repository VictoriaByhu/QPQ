using MediatR;
using QPQ.Reviews.Application.Interfaces;
using QPQ.Reviews.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Commands.CreateReview;

public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, string>
{
    private readonly IReviewRepository _repository;

    public CreateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = new Review(request.ProductId, request.Author, request.Rating, request.Comment);

        await _repository.AddAsync(review);

        return review.Id.ToString();
    }
}