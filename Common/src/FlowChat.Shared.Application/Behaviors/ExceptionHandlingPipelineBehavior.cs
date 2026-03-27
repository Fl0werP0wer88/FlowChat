using System.Diagnostics;
using FluentValidation;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using DomainValidationException = FlowChat.Shared.Domain.Exceptions.ValidationException;

namespace FlowChat.Shared.Application.Behaviors;

public sealed class ExceptionHandlingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull, IFlowChatResult, IFlowChatResultFactory<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (DomainValidationException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "validation_failed");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "validation");
            Activity.Current?.SetTag("validation.error_count", exception.Errors.Count);

            var domainError = DomainError.Validation(exception.Message, exception.Errors.ToList());
            return TResponse.Failure(domainError);
        }
        catch (ValidationException exception)
        {
            var errors = exception.Errors
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .ToList();

            Activity.Current?.SetStatus(ActivityStatusCode.Error, "validation_failed");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "validation");
            Activity.Current?.SetTag("validation.error_count", errors.Count);

            var domainError = DomainError.Validation(exception.Message, errors);
            return TResponse.Failure(domainError);
        }
        catch (FlowChatException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "bad_request");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "bad_request");

            var domainError = DomainError.BadRequest(exception.Message);
            return TResponse.Failure(domainError);
        }
        catch (Exception exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "unexpected");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "unexpected");

            var domainError = DomainError.UnExpected("An unexpected error occurred.");
            return TResponse.Failure(domainError);
        }
    }
}

