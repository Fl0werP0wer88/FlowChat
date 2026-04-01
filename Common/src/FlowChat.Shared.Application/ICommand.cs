using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

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

