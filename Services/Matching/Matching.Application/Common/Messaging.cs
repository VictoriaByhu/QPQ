using MediatR;

namespace Matching.Application.Common;

/// <summary>Команда змінює стан і не повертає даних.</summary>
public interface ICommand : IRequest { }

/// <summary>Команда змінює стан і повертає результат (наприклад, Id або нову версію).</summary>
public interface ICommand<TResponse> : IRequest<TResponse> { }

/// <summary>Запит лише читає дані.</summary>
public interface IQuery<TResponse> : IRequest<TResponse> { }
