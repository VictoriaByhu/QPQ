using MediatR;
using QPQ.Reviews.Domain.ValueObjects;
using QPQ.Reviews.Application.Common.Interfaces;

namespace QPQ.Reviews.Application.Reviews.Commands.CreateReview;

// Переконайся, що твої параметри співпадають, головне тут — IRequest<string>
public record CreateReviewCommand(string ProductId, Author Author, int Rating, string Comment) : ICommand<string>;