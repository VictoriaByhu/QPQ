using MediatR;
using QPQ.Reviews.Application.Common.Interfaces;
using QPQ.Reviews.Domain.Entities;

namespace QPQ.Reviews.Application.Reviews.Queries.GetReviewById;

public record GetReviewByIdQuery(string Id) : IQuery<ReviewDto?>;