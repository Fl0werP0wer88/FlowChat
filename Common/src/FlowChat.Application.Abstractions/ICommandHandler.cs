using FlowChat.Core.Results;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, FlowChatResult<TResponse>>
    where TRequest : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
}

public interface ICommandHandler<TRequest> : IRequestHandler<TRequest, Result<Unit>>
    where TRequest : ICommand
{
}
