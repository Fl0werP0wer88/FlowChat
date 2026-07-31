using FlowChat.RealtimeService.Consumers.Configuration.Settings;
using FlowChat.RealtimeService.Consumers.Kafka.Retry;
using FluentAssertions;

namespace FlowChat.RealtimeService.UnitTests.Worker.Kafka;

public sealed class RealtimeRetryTopologyTests
{
    [Fact]
    public void Constructor_ValidSettings_IndexesEveryRetryTier()
    {
        var settings = CreateSettings();

        var topology = new RealtimeRetryTopology([settings]);

        settings.RetryTiers.Select((tier, index) => (tier, index)).Should().AllSatisfy(item =>
        {
            topology.TryGetRetryTopic(item.tier.Topic, out var registration).Should().BeTrue();
            registration.Stream.Should().BeSameAs(settings);
            registration.TierIndex.Should().Be(item.index);
        });
    }

    [Fact]
    public void Constructor_DuplicateRetryTopic_ThrowsInvalidOperationException()
    {
        var settings = new ChatMessageV2ConsumerSettingsSection
        {
            RetryTiers =
            [
                new RetryTierSettings { Topic = "retry", Delay = TimeSpan.FromSeconds(5) },
                new RetryTierSettings { Topic = "retry", Delay = TimeSpan.FromSeconds(20) }
            ]
        };

        var action = () => new RealtimeRetryTopology([settings]);

        action.Should().Throw<InvalidOperationException>().WithMessage("*non-empty and unique*");
    }

    [Fact]
    public void Constructor_NonIncreasingDelay_ThrowsInvalidOperationException()
    {
        var settings = new ChatMessageV2ConsumerSettingsSection
        {
            RetryTiers =
            [
                new RetryTierSettings { Topic = "retry-1", Delay = TimeSpan.FromSeconds(20) },
                new RetryTierSettings { Topic = "retry-2", Delay = TimeSpan.FromSeconds(5) }
            ]
        };

        var action = () => new RealtimeRetryTopology([settings]);

        action.Should().Throw<InvalidOperationException>().WithMessage("*strictly increasing*");
    }

    private static ChatMessageV2ConsumerSettingsSection CreateSettings() => new()
    {
        RetryTiers =
        [
            new RetryTierSettings { Topic = "retry-5s", Delay = TimeSpan.FromSeconds(5) },
            new RetryTierSettings { Topic = "retry-20s", Delay = TimeSpan.FromSeconds(20) },
            new RetryTierSettings { Topic = "retry-60s", Delay = TimeSpan.FromSeconds(60) },
            new RetryTierSettings { Topic = "retry-300s", Delay = TimeSpan.FromSeconds(300) }
        ]
    };
}
