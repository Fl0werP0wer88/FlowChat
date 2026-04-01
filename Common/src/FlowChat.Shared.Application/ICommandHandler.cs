using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

public interface ICommandHandler<TRequest, TResponse> : IRequestHandler<TRequest, FlowChatResult<TResponse>>
    where TRequest : ICommand<TResponse>, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
}

public interface ICommandHandler<TRequest> : IRequestHandler<TRequest, Result<Unit>>
    where TRequest : ICommand
{
}

