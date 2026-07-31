using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

public sealed class TieredKafkaRetryTopologyTests
{
    [Fact]
    public void Constructor_ValidSettings_IndexesEveryRetryTier()
    {
        var settings = CreateSettings();

        var topology = new TieredKafkaRetryTopology([settings]);

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
        var settings = new TestTieredRetryKafkaConsumerSettingsSection
        {
            RetryTiers =
            [
                new RetryTierSettings { Topic = "retry", Delay = TimeSpan.FromSeconds(5) },
                new RetryTierSettings { Topic = "retry", Delay = TimeSpan.FromSeconds(20) }
            ]
        };

        var action = () => new TieredKafkaRetryTopology([settings]);

        action.Should().Throw<InvalidOperationException>().WithMessage("*non-empty and unique*");
    }

    [Fact]
    public void Constructor_NonIncreasingDelay_ThrowsInvalidOperationException()
    {
        var settings = new TestTieredRetryKafkaConsumerSettingsSection
        {
            RetryTiers =
            [
                new RetryTierSettings { Topic = "retry-1", Delay = TimeSpan.FromSeconds(20) },
                new RetryTierSettings { Topic = "retry-2", Delay = TimeSpan.FromSeconds(5) }
            ]
        };

        var action = () => new TieredKafkaRetryTopology([settings]);

        action.Should().Throw<InvalidOperationException>().WithMessage("*strictly increasing*");
    }

    private static TestTieredRetryKafkaConsumerSettingsSection CreateSettings() =>
        TestTieredRetryKafkaConsumerSettingsSection.Create();
}
