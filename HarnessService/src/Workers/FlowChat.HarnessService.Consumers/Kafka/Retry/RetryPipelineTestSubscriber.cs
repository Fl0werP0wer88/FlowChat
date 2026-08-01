using FlowChat.Core.Exceptions;
using FlowChat.HarnessService.Persistence;
using FlowChat.HarnessService.Persistence.Entities.Retry;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Subscribers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FlowChat.HarnessService.Consumers.Kafka.Retry;

public sealed class RetryPipelineTestSubscriber(
    RetryPipelineTestAttemptTracker attemptTracker,
    AppDbContext dbContext,
    IUnitOfWork unitOfWork,
    IConsumedOffsetCommitter offsetCommitter,
    TimeProvider timeProvider,
    ILogger<RetryPipelineTestSubscriber> logger)
    : SubscriberBase<RetryPipelineTestIntegrationEvent>(logger)
{
    protected override async Task ExecuteAsync(
        RetryPipelineTestIntegrationEvent message,
        CancellationToken cancellationToken)
    {
        var attempt = attemptTracker.RegisterAttempt(message.ScenarioId);
        if (attempt <= message.FailuresBeforeSuccess)
            ThrowConfiguredFailure(message.FailureKind, message.ScenarioId, attempt);

        await unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                if (!await dbContext.RetryPipelineTestResults.AnyAsync(
                        x => x.ScenarioId == message.ScenarioId,
                        transactionCancellationToken))
                {
                    dbContext.RetryPipelineTestResults.Add(new RetryPipelineTestResultEntity
                    {
                        ScenarioId = message.ScenarioId,
                        AttemptCount = attempt,
                        CompletedAtUtc = timeProvider.GetUtcNow()
                    });
                }

                await unitOfWork.SaveChangesAsync(transactionCancellationToken);
                await offsetCommitter.CommitConsumedOffsetsAsync(transactionCancellationToken);
                return true;
            },
            cancellationToken);
    }

    private static void ThrowConfiguredFailure(
        RetryPipelineTestFailureKind failureKind,
        Guid scenarioId,
        int attempt)
    {
        var message = $"Configured {failureKind} failure for scenario {scenarioId} at attempt {attempt}.";
        throw failureKind switch
        {
            RetryPipelineTestFailureKind.Transient => new TransientException(message),
            RetryPipelineTestFailureKind.Isolable => new IsolableException(message),
            _ => new NonTransientException(message)
        };
    }
}
