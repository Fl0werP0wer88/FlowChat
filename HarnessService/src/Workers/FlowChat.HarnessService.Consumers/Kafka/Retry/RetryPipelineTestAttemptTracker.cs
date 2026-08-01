using System.Collections.Concurrent;

namespace FlowChat.HarnessService.Consumers.Kafka.Retry;

public sealed class RetryPipelineTestAttemptTracker
{
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();

    public int RegisterAttempt(Guid scenarioId) =>
        _attempts.AddOrUpdate(scenarioId, 1, (_, current) => current + 1);

    public int GetAttemptCount(Guid scenarioId) =>
        _attempts.TryGetValue(scenarioId, out var count) ? count : 0;
}
