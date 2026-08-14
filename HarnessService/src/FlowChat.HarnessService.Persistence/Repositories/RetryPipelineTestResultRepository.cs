using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.HarnessService.Persistence.Entities.Retry;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.HarnessService.Persistence.Repositories;

public sealed class RetryPipelineTestResultRepository(AppDbContext dbContext)
    : IRetryPipelineTestResultRepository
{
    public async Task AddIfMissingAsync(
        Guid scenarioId,
        int attemptCount,
        DateTimeOffset completedAtUtc,
        CancellationToken cancellationToken)
    {
        if (await dbContext.RetryPipelineTestResults.AnyAsync(
                result => result.ScenarioId == scenarioId,
                cancellationToken))
        {
            return;
        }

        dbContext.RetryPipelineTestResults.Add(new RetryPipelineTestResultEntity
        {
            ScenarioId = scenarioId,
            AttemptCount = attemptCount,
            CompletedAtUtc = completedAtUtc
        });
    }
}
