namespace Matching.Domain.Exceptions;

/// <summary>Порушення бізнес-правила в доменній моделі (-> 400).</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Конфлікт версій документа або інший конфлікт стану (-> 409).</summary>
public class ConflictException(string message) : Exception(message);
