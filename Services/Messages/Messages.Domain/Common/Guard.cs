using MongoDB.Bson;
using Messages.Domain.Exceptions;

namespace Messages.Domain.Common;

public static class Guard
{
    public static string UserId(string? value, string name)
    {
        if (!Guid.TryParse(value, out var id) || id == Guid.Empty)
            throw new DomainException($"{name} must be a non-empty GUID.");
        return id.ToString("D");
    }

    public static ObjectId Id(ObjectId value, string name)
    {
        if (value == ObjectId.Empty)
            throw new DomainException($"{name} must be specified.");
        return value;
    }

    public static string Text(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException($"{name} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{name} must not exceed {maxLength} characters.");
        return trimmed;
    }
}
