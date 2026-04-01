using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

public interface IQuery<TResponse> : IRequestBase, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
}

public interface IQuery : IRequestBase, IRequest<Result>
{
}

