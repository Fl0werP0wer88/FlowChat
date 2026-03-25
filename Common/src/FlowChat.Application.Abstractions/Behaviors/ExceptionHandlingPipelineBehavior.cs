using System.Diagnostics;
using FluentValidation;
using FlowChat.Core.Exceptions;
using FlowChat.Domain.Abstractions;
using MediatR;
using DomainValidationException = FlowChat.Domain.Abstractions.Exceptions.ValidationException;

namespace FlowChat.Application.Abstractions.Behaviors;

public sealed class ExceptionHandlingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, Result<TResponse, IDomainError>>
    where TRequest : notnull, IRequest<Result<TResponse, IDomainError>>
    where TResponse : notnull
{
    public async Task<Result<TResponse, IDomainError>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse, IDomainError>> next,
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
            return Result.Failure<TResponse, IDomainError>(domainError);
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
            return Result.Failure<TResponse, IDomainError>(domainError);
        }
        catch (FlowChatException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "bad_request");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "bad_request");

            var domainError = DomainError.BadRequest(exception.Message);
            return Result.Failure<TResponse, IDomainError>(domainError);
        }
        catch (Exception exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "unexpected");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "unexpected");

            var domainError = DomainError.UnExpected("An unexpected error occurred.");
            return Result.Failure<TResponse, IDomainError>(domainError);
        }
    }
}
