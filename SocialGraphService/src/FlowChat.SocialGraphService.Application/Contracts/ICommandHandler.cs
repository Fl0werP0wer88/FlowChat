using FlowChat.SocialGraphService.Domain.Errors;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contracts
{
    public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, Result<TResponse, IDomainError>>
        where TRequest : ICommand<TResponse>, IRequest<Result<TResponse, IDomainError>>
        where TResponse : notnull
    { }

    public interface ICommandHandler<TRequest> : IRequestHandler<TRequest, Result<Unit>>
        where TRequest : ICommand
    { }
}