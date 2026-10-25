namespace Messages.Domain.Exceptions;

public sealed class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ConflictException(string message) : Exception(message);
