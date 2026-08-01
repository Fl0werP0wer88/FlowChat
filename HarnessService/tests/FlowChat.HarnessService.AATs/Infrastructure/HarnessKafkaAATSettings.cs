namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed record ProjectionKafkaAATSettings(
    string Topic,
    string RetryTopic,
    string DeadLetterTopic);

public sealed record RetryPipelineKafkaAATSettings(
    string Topic,
    IReadOnlyList<RetryPipelineTierAATSettings> RetryTiers,
    string DeadLetterTopic);

public sealed record RetryPipelineTierAATSettings(string Topic, TimeSpan Delay);
