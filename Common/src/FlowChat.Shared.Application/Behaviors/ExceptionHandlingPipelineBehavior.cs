using System.Data.Common;
using System.Diagnostics;
using FluentValidation;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        // Separate catch for FluentValidation — domain layer and FluentValidation have distinct exception types.
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
        catch (TransientException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "transient_failure");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "transient");

            var domainError = DomainError.UnExpected("An unexpected error occurred.", FailureKind.Transient);
            return TResponse.Failure(domainError);
        }
        catch (IsolableException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "isolable_failure");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "isolable");

            var domainError = DomainError.UnExpected(exception.Message, FailureKind.Isolable);
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
        catch (DbUpdateConcurrencyException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "db_concurrency_failed");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "db_concurrency");
            Activity.Current?.SetTag("db.exception.transient", false);
            Activity.Current?.SetTag("db.exception.type", exception.GetType().Name);
            Activity.Current?.SetTag("db.concurrency.entry_count", exception.Entries.Count);

            foreach (var entry in exception.Entries)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);

                if (databaseValues is null)
                {
                    //ToDo: Think about concurrency handling for missing or deleted rows
                }
                else
                {
                    //ToDo: Think about optimistic concurrency handling for existing rows
                }
            }

            var domainError = DomainError.UnExpected("An unexpected error occurred.");
            return TResponse.Failure(domainError);
        }
        catch (DbUpdateException exception)
        {
            var dbException = exception.InnerException as DbException;
            var isTransient = dbException?.IsTransient == true;

            Activity.Current?.SetStatus(ActivityStatusCode.Error, "db_update_failed");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "db_update");
            Activity.Current?.SetTag("db.exception.transient", isTransient);
            Activity.Current?.SetTag("db.exception.type", exception.GetType().Name);

            if (!string.IsNullOrWhiteSpace(dbException?.SqlState))
            {
                Activity.Current?.SetTag("db.exception.sql_state", dbException.SqlState);
            }

            var domainError = DomainError.UnExpected("An unexpected error occurred.", isTransient ? FailureKind.Transient : FailureKind.None);
            return TResponse.Failure(domainError);
        }
        catch (OperationCanceledException exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "request_canceled");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "canceled");

            var domainError = DomainError.BadRequest("The request was canceled.");
            return TResponse.Failure(domainError);
        }
        catch (Exception exception)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "unexpected");
            Activity.Current?.AddException(exception);
            Activity.Current?.SetTag("error.type", "unexpected");

            // Generic message intentionally hides internal details from external callers.
            var domainError = DomainError.UnExpected("An unexpected error occurred.");
            return TResponse.Failure(domainError);
        }
    }
}

