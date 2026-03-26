using FlowChat.Core.Results;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface IRequestBase
{
}

public interface ICommand<TResponse> : IRequestBase, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
}

public interface ICommand : IRequestBase, IRequest<Result<Unit>>
{
}
