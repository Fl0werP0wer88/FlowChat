using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application.Features.KafkaRetry;

namespace FlowChat.HarnessService.Consumers.Kafka.Retry;

public sealed record RetryPipelineTestIntegrationEvent : IntegrationEvent
{
    public required Guid ScenarioId { get; init; }
    public required RetryPipelineTestFailureKind FailureKind { get; init; }
    public required int FailuresBeforeSuccess { get; init; }
}
