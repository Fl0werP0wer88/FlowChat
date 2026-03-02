using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contracts
{
    public interface IRequestBase { }
    public interface IQuery<TResponse> : IRequestBase, IRequest<Result<TResponse, IDomainError>>
        where TResponse : notnull
    { }

    public interface IQuery : IRequestBase, IRequest<Result>
    { }
}

