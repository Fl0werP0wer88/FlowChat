using FlowChat.Core.Exceptions;
using FlowChat.RealtimeService.Consumers.Configuration.Settings;

namespace FlowChat.RealtimeService.Consumers.Kafka.Retry;

public static class RetryFailureRouter
{
    public static RetryDestination Resolve(
        ITieredRetryKafkaConsumerSettingsSection settings,
        int? sourceRetryTierIndex,
        Exception exception)
    {
        if (sourceRetryTierIndex is null && exception is TransientException or IsolableException)
            return CreateRetryDestination(settings, 0);

        if (sourceRetryTierIndex is int currentTier &&
            exception is TransientException &&
            currentTier + 1 < settings.RetryTiers.Count)
        {
            return CreateRetryDestination(settings, currentTier + 1);
        }

        return new RetryDestination(settings.DeadLetterTopic, null, null, true);
    }

    private static RetryDestination CreateRetryDestination(
        ITieredRetryKafkaConsumerSettingsSection settings,
        int tierIndex)
    {
        var tier = settings.RetryTiers[tierIndex];
        return new RetryDestination(tier.Topic, tier.Delay, tierIndex + 1, false);
    }
}

public sealed record RetryDestination(
    string Topic,
    TimeSpan? Delay,
    int? Attempt,
    bool IsDeadLetter);
