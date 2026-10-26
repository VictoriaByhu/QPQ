using QPQ.Reviews.Domain.Common;
using System;
using System.Collections.Generic;

namespace QPQ.Reviews.Domain.ValueObjects;

public sealed class Author : ValueObject
{
    public string UserId { get; }
    public string Username { get; }

    public Author(string userId, string username)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("UserId is required");
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username is required");

        UserId = userId;
        Username = username;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return UserId;
        yield return Username;
    }
}