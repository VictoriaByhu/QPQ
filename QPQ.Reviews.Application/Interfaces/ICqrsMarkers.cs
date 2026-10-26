using MediatR;

namespace QPQ.Reviews.Application.Common.Interfaces;

public interface ICommand : IRequest { }
public interface ICommand<out TResponse> : IRequest<TResponse> { }
public interface IQuery<out TResponse> : IRequest<TResponse> { }