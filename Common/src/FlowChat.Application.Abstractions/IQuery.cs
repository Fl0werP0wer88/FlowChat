using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface IQuery<TResponse> : IRequestBase, IRequest<Result<TResponse, IDomainError>>
    where TResponse : notnull
{
}

public interface IQuery : IRequestBase, IRequest<Result>
{
}
