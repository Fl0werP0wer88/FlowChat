using FlowChat.SocialGraphService.Domain.Errors;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contracts
{
    public interface ICommand<TResponse> : IRequestBase, IRequest<Result<TResponse, IDomainError>>
        where TResponse : notnull
    { }

    public interface ICommand : IRequestBase, IRequest<Result<Unit>> { }
}
