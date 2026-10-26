namespace Matching.Application.Common.Exceptions;

/// <summary>Документ не знайдено (-> 404).</summary>
public sealed class NotFoundException(string entity, string key)
    : Exception($"{entity} '{key}' was not found");
