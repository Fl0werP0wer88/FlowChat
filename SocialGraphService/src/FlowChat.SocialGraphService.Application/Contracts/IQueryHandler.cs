using FlowChat.SocialGraphService.Domain.Errors;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Contracts;

public interface IQueryHandler<TRequest, TResponse> : IRequestHandler<TRequest, Result<TResponse, IDomainError>>
   where TRequest : IQuery<TResponse>
   where TResponse : notnull
{ }