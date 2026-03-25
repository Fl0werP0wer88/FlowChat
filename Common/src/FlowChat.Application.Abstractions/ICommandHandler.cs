using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, Result<TResponse, IDomainError>>
    where TRequest : ICommand<TResponse>, IRequest<Result<TResponse, IDomainError>>
    where TResponse : notnull
{
}

public interface ICommandHandler<TRequest> : IRequestHandler<TRequest, Result<Unit, IDomainError>>
    where TRequest : ICommand
{
}
