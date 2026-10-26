using MediatR;
using QPQ.Reviews.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace QPQ.Reviews.Application.Reviews.Commands.UpdateReview;

public class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand>
{
    private readonly IReviewRepository _repository;

    public UpdateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await _repository.GetByIdAsync(request.Id);

        if (review != null)
        {
            review.Rating = request.Rating;
            review.Comment = request.Comment;

            await _repository.UpdateAsync(review);
        }
    }
}