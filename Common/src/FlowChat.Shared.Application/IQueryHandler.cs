using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.Shared.Application;

public interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, FlowChatResult<TResponse>>
    where TRequest : IQuery<TResponse>
    where TResponse : notnull
{
}

