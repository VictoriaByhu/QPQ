namespace UserSkills.Domain.Exceptions;

/// <summary>Ресурс не знайдено (HTTP 404).</summary>
public sealed class NotFoundException(string message) : Exception(message);

/// <summary>Порушено бізнес-правило, конфлікт стану або версії (HTTP 409).</summary>
public sealed class BusinessConflictException(string message) : Exception(message);

/// <summary>Некоректні вхідні дані (HTTP 400).</summary>
public sealed class ValidationException(string message) : Exception(message);

/// <summary>Дію може виконати лише власник запису (HTTP 403).</summary>
public sealed class ForbiddenException(string message) : Exception(message);
