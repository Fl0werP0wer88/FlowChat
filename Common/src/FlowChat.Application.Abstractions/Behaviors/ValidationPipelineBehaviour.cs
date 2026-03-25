using System.Diagnostics;
using FluentValidation;
using MediatR;
using DomainValidationException = FlowChat.Domain.Abstractions.Exceptions.ValidationException;

namespace FlowChat.Application.Abstractions.Behaviors;

public sealed class ValidationPipelineBehaviour<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);

        var results = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToList();

        if (failures.Count > 0)
        {
            Activity.Current?.SetTag("validation.failed", true);
            Activity.Current?.SetTag("validation.error_count", failures.Count);

            var distinctProperties = failures
                .Select(failure => failure.PropertyName)
                .Where(propertyName => !string.IsNullOrWhiteSpace(propertyName))
                .Distinct()
                .Take(10)
                .ToArray();

            if (distinctProperties.Length > 0)
            {
                Activity.Current?.SetTag("validation.properties", string.Join(",", distinctProperties));
            }

            throw new DomainValidationException(failures.Select(failure => failure.ErrorMessage).ToList());
        }

        return await next(cancellationToken);
    }
}
