using MediatR;

namespace QPQ.Reviews.Application.Reviews.Commands.DeleteReview;

public record DeleteReviewCommand(string Id) : IRequest;