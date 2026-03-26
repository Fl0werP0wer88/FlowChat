using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface IRequestBase
{
}

public interface ICommand<TResponse> : IRequestBase, IRequest<Result<TResponse, IDomainError>>
    where TResponse : notnull
{
}

public interface ICommand : IRequestBase, IRequest<Result<Unit>>
{
}
