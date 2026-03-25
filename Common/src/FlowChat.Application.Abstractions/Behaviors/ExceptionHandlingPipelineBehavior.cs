using System.Diagnostics;
using System.Reflection;
using FluentValidation;
using FlowChat.Core.Exceptions;
using FlowChat.Domain.Abstractions;
using MediatR;
using DomainValidationException = FlowChat.Domain.Abstractions.Exceptions.ValidationException;

namespace FlowChat.Application.Abstractions.Behaviors;

public sealed class ExceptionHandlingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    private static readonly MethodInfo FailureWithDomainErrorMethod = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method =>
            method.Name == nameof(Result.Failure)
            && method.IsGenericMethodDefinition
            && method.GetGenericArguments().Length == 2
            && method.GetParameters().Length == 1);

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
            return CastOrThrow(domainError, exception);
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
            return CastOrThrow(domainError, exception);
        }
        catch (FlowChatException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "bad_request");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "bad_request");

            var domainError = DomainError.BadRequest(exception.Message);
            return CastOrThrow(domainError, exception);
        }
        catch (Exception exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "unexpected");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "unexpected");

            var domainError = DomainError.UnExpected("An unexpected error occurred.");
            return CastOrThrow(domainError, exception);
        }
    }

    private static TResponse CastOrThrow(IDomainError domainError, Exception exception)
    {
        var responseType = typeof(TResponse);

        if (responseType.IsGenericType
            && responseType.GetGenericTypeDefinition() == typeof(Result<,>))
        {
            var genericArguments = responseType.GetGenericArguments();
            if (genericArguments[1] == typeof(IDomainError))
            {
                var failureResult = FailureWithDomainErrorMethod
                    .MakeGenericMethod(genericArguments[0], typeof(IDomainError))
                    .Invoke(null, [domainError]);

                if (failureResult is TResponse response)
                {
                    return response;
                }
            }
        }

        throw new InvalidCastException(
            $"Failed to cast failure result to {responseType.Name} for request {typeof(TRequest).Name}.",
            exception);
    }
}
