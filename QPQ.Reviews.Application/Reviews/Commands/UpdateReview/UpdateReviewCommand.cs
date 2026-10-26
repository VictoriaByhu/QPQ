using MediatR;
using QPQ.Reviews.Application.Common.Interfaces;

namespace QPQ.Reviews.Application.Reviews.Commands.UpdateReview;

public record UpdateReviewCommand(string Id, int Rating, string Comment) : ICommand;