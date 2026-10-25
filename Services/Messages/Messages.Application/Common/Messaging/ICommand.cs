using MediatR;

namespace Messages.Application.Common.Messaging;

public interface ICommand : IRequest;

public interface ICommand<out TResponse> : IRequest<TResponse>;

public interface IQuery<out TResponse> : IRequest<TResponse>;
