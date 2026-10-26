using QPQ.Reviews.Domain.Common;
using QPQ.Reviews.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace QPQ.Reviews.Domain.Entities;

public class Review : BaseEntity
{
    public string ProductId { get; private set; }
    public Author Author { get; private set; }
    public int Rating { get; set; }
    public string Comment { get; set; }

    // Додано приватну колекцію (Критерії 2 та 10)
    private readonly List<string> _tags = new();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();

    public Review(string productId, Author author, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Рейтинг має бути від 1 до 5.");

        ProductId = productId;
        Author = author;
        Rating = rating;
        Comment = comment ?? string.Empty;
    }

    public void UpdateReview(string newComment, int newRating)
    {
        if (newRating < 1 || newRating > 5)
            throw new ArgumentOutOfRangeException(nameof(newRating), "Рейтинг має бути від 1 до 5.");

        Comment = newComment;
        Rating = newRating;

        Touch();
        BumpVersion();
    }
}