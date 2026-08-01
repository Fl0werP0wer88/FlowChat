using FlowChat.Core.Messaging;

namespace FlowChat.HarnessService.Consumers.Kafka.Retry;

public sealed record RetryPipelineTestIntegrationEvent : IntegrationEvent
{
    public required Guid ScenarioId { get; init; }
    public required RetryPipelineTestFailureKind FailureKind { get; init; }
    public required int FailuresBeforeSuccess { get; init; }
}

public enum RetryPipelineTestFailureKind
{
    Transient,
    Isolable,
    NonTransient
}
