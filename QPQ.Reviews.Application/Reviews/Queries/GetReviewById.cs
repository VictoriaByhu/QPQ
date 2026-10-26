using System;

namespace QPQ.Reviews.Application.Reviews.Queries.GetReviewById;

public record ReviewDto(
    string Id,
    string ProductId,
    string AuthorUserId,
    string AuthorUsername,
    int Rating,
    string Comment,
    DateTime CreatedAt
);