using System.Diagnostics;
using FlowChat.Shared.Application.Observability;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace FlowChat.Shared.Application.Behaviors;

public sealed class LoggingPipelineBehaviour<TRequest, TResponse>(
    ILogger<LoggingPipelineBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull, IFlowChatResult
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromSeconds(5);
    private static readonly string RequestKind = ResolveRequestKind();

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var activity = ApplicationActivitySource.For<TRequest>().StartActivity(
            requestName,
            ActivityKind.Internal);

        activity?.SetTag("messaging.system", "mediatr");
        activity?.SetTag("request.name", requestName);
        activity?.SetTag("request.kind", RequestKind);
        activity?.SetTag("layer", "application");

        logger.LogInformation(
            "{RequestKind} started: {RequestName}",
            RequestKind,
            requestName);

        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(cancellationToken);
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            if (elapsed > SlowRequestThreshold)
            {
                logger.LogWarning(
                    "Slow {RequestKind} detected: {RequestName}. Duration: {DurationMs} ms",
                    RequestKind,
                    requestName,
                    elapsed.TotalMilliseconds);
            }

            if (response.IsFailure)
            {
                var domainError = response.Error;
                activity?.SetStatus(ActivityStatusCode.Error, domainError.ErrorMessage);
                activity?.SetTag("error.type", ResolveErrorTypeTag(domainError.ErrorType));

                if (domainError.Errors?.Count > 0)
                {
                    activity?.SetTag("error.count", domainError.Errors.Count);
                }

                if (domainError.ErrorType == ErrorType.Unexpected)
                {
                    logger.LogError(
                        "{RequestKind} failed: {RequestName}. Duration: {DurationMs} ms. ErrorType: {ErrorType}. Message: {ErrorMessage}",
                        RequestKind,
                        requestName,
                        elapsed.TotalMilliseconds,
                        domainError.ErrorType.Name,
                        domainError.ErrorMessage);
                }
                else
                {
                    logger.LogWarning(
                        "{RequestKind} failed: {RequestName}. Duration: {DurationMs} ms. ErrorType: {ErrorType}. Message: {ErrorMessage}",
                        RequestKind,
                        requestName,
                        elapsed.TotalMilliseconds,
                        domainError.ErrorType.Name,
                        domainError.ErrorMessage);
                }

                return response;
            }

            activity?.SetStatus(ActivityStatusCode.Ok);

            logger.LogInformation(
                "{RequestKind} completed: {RequestName}. Duration: {DurationMs} ms",
                RequestKind,
                requestName,
                elapsed.TotalMilliseconds);

            return response;
        }
        catch (Exception exception)
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);

            activity?.SetStatus(ActivityStatusCode.Error, exception.Message);
            activity?.AddException(exception);

            logger.LogError(
                exception,
                "{RequestKind} failed: {RequestName}. Duration: {DurationMs} ms",
                RequestKind,
                requestName,
                elapsed.TotalMilliseconds);

            throw;
        }
    }

    private static string ResolveRequestKind()
    {
        var requestType = typeof(TRequest);
        var interfaces = requestType.GetInterfaces();

        if (typeof(ICommand).IsAssignableFrom(requestType)
            || interfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ICommand<>)))
        {
            return "command";
        }

        if (typeof(IQuery).IsAssignableFrom(requestType)
            || interfaces.Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IQuery<>)))
        {
            return "query";
        }

        return "request";
    }

    private static string ResolveErrorTypeTag(ErrorType errorType) =>
        errorType.Name.ToLowerInvariant();
}

