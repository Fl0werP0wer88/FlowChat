using FlowChat.Core.Results;
using FlowChat.Domain.Abstractions;
using MediatR;

namespace FlowChat.Application.Abstractions;

public interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, FlowChatResult<TResponse>>
    where TRequest : IQuery<TResponse>
    where TResponse : notnull
{
}
