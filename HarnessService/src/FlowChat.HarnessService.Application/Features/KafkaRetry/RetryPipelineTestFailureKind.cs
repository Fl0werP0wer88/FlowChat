namespace FlowChat.HarnessService.Application.Features.KafkaRetry;

public enum RetryPipelineTestFailureKind
{
    Transient,
    Isolable,
    NonTransient
}
