using FlowChat.Core.Results;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface IQuery<TResponse> : IRequestBase, IRequest<FlowChatResult<TResponse>>
    where TResponse : notnull
{
}

public interface IQuery : IRequestBase, IRequest<Result>
{
}
