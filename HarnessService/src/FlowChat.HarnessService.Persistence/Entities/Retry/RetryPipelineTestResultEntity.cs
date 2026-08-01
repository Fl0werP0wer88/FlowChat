namespace FlowChat.HarnessService.Persistence.Entities.Retry;

public sealed class RetryPipelineTestResultEntity
{
    public Guid ScenarioId { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CompletedAtUtc { get; set; }
}
