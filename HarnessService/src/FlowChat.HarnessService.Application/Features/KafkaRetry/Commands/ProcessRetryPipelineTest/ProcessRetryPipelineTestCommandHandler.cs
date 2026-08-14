using CSharpFunctionalExtensions;
using FlowChat.Core.Exceptions;
using FlowChat.Core.Results;
using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.HarnessService.Application.Features.KafkaRetry.Commands.ProcessRetryPipelineTest;

public sealed class ProcessRetryPipelineTestCommandHandler(
    RetryPipelineTestAttemptTracker attemptTracker,
    IRetryPipelineTestResultRepository resultRepository,
    TimeProvider timeProvider,
    IUnitOfWork unitOfWork)
    : TransactionalCommandHandlerBase<ProcessRetryPipelineTestCommand, Unit>(unitOfWork)
{
    protected override async Task<FlowChatResult<Unit>> HandleInTransactionAsync(
        ProcessRetryPipelineTestCommand request,
        CancellationToken cancellationToken)
    {
        var attempt = attemptTracker.RegisterAttempt(request.ScenarioId);
        if (attempt <= request.FailuresBeforeSuccess)
            ThrowConfiguredFailure(request.FailureKind, request.ScenarioId, attempt);

        await resultRepository.AddIfMissingAsync(
            request.ScenarioId,
            attempt,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return FlowChatResult<Unit>.Success(Unit.Value);
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
