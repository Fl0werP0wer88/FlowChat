namespace FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;

public sealed class TieredKafkaRetryTopology
{
    private readonly IReadOnlyDictionary<string, RetryTopicRegistration> _retryTopics;

    public TieredKafkaRetryTopology(IEnumerable<ITieredRetryKafkaConsumerSettingsSection> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var streams = settings.ToArray();
        foreach (var stream in streams)
            Validate(stream);

        Streams = streams;
        _retryTopics = streams
            .SelectMany(stream => stream.RetryTiers.Select((tier, index) =>
                new RetryTopicRegistration(stream, tier, index)))
            .ToDictionary(registration => registration.Tier.Topic, StringComparer.Ordinal);
    }

    public IReadOnlyList<ITieredRetryKafkaConsumerSettingsSection> Streams { get; }

    public bool TryGetRetryTopic(string topic, out RetryTopicRegistration registration) =>
        _retryTopics.TryGetValue(topic, out registration!);

    private static void Validate(ITieredRetryKafkaConsumerSettingsSection settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Topic))
            throw new InvalidOperationException("The main Kafka topic is required.");
        if (string.IsNullOrWhiteSpace(settings.DeadLetterTopic))
            throw new InvalidOperationException($"The DLQ topic for '{settings.Topic}' is required.");
        if (settings.RetryTiers.Count == 0)
            throw new InvalidOperationException($"At least one retry tier is required for '{settings.Topic}'.");

        var topics = new HashSet<string>(StringComparer.Ordinal);
        TimeSpan previousDelay = TimeSpan.Zero;
        foreach (var tier in settings.RetryTiers)
        {
            if (string.IsNullOrWhiteSpace(tier.Topic) || !topics.Add(tier.Topic))
                throw new InvalidOperationException($"Retry topics for '{settings.Topic}' must be non-empty and unique.");
            if (tier.Delay <= previousDelay)
                throw new InvalidOperationException($"Retry delays for '{settings.Topic}' must be positive and strictly increasing.");

            previousDelay = tier.Delay;
        }
    }
}

public sealed record RetryTopicRegistration(
    ITieredRetryKafkaConsumerSettingsSection Stream,
    RetryTierSettings Tier,
    int TierIndex);
