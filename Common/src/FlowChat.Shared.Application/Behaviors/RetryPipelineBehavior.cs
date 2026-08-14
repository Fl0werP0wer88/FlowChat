using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace FlowChat.Shared.Application.Behaviors;

public sealed class RetryPipelineBehavior<TRequest, TResponse>(
    ILogger<RetryPipelineBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull, IFlowChatResult
{
    private const int MaxRetryAttempts = 2;
    private static readonly TimeSpan InitialRetryDelay = TimeSpan.FromMilliseconds(200);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not IInProcessRetryableRequest)
        {
            return await next(cancellationToken);
        }

        var pipeline = new ResiliencePipelineBuilder<TResponse>()
            .AddRetry(new RetryStrategyOptions<TResponse>
            {
                MaxRetryAttempts = MaxRetryAttempts,
                Delay = InitialRetryDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<TResponse>()
                    .Handle<TransientException>()
                    .Handle<DbUpdateConcurrencyException>()
                    .HandleResult(IsTransientFailure),
                OnRetry = arguments =>
                {
                    logger.LogWarning(
                        "Retrying request {RequestName}. Retry attempt: {RetryAttempt} of {MaxRetryAttempts}. Delay: {RetryDelayMs} ms",
                        typeof(TRequest).Name,
                        arguments.AttemptNumber + 1,
                        MaxRetryAttempts,
                        arguments.RetryDelay.TotalMilliseconds);

                    return ValueTask.CompletedTask;
                }
            })
            .Build();

        return await pipeline.ExecuteAsync(
            async token => await next(token),
            cancellationToken);
    }

    private static bool IsTransientFailure(TResponse response) =>
        response.IsFailure && response.Error.FailureKind == FailureKind.Transient;
}
