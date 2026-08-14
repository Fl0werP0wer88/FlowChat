namespace FlowChat.HarnessService.Application.Contracts.Persistence;

public interface IRetryPipelineTestResultRepository
{
    Task AddIfMissingAsync(
        Guid scenarioId,
        int attemptCount,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken);
}
