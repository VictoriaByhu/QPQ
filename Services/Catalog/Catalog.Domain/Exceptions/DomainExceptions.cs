namespace Catalog.Domain.Exceptions;

/// <summary>Порушено інваріант сутності (HTTP 409).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Ресурс не знайдено (HTTP 404).</summary>
public sealed class EntityNotFoundException(string message) : Exception(message);

/// <summary>Конфлікт стану: дублікат, зміна іншим користувачем, залежні записи (HTTP 409).</summary>
public sealed class ConflictException(string message) : Exception(message);

/// <summary>
/// Некоректні вхідні дані, які не вдалося перевірити без звернення до бази (HTTP 400).
/// Помилки групуються за полями, як у ValidationProblemDetails.
/// </summary>
public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]> { [field] = [message] }) { }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.") => Errors = errors;
}
