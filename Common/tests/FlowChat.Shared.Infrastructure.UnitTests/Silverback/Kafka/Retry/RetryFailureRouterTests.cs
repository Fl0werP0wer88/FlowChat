using FlowChat.Core.Exceptions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry;
using FluentAssertions;

namespace FlowChat.Shared.Infrastructure.UnitTests.Silverback.Kafka.Retry;

public sealed class RetryFailureRouterTests
{
    private readonly TestTieredRetryKafkaConsumerSettingsSection _settings =
        TestTieredRetryKafkaConsumerSettingsSection.Create();

    [Theory]
    [InlineData(typeof(TransientException))]
    [InlineData(typeof(IsolableException))]
    public void Resolve_MainRetryableFailure_SelectsFirstTier(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "failure")!;

        var result = RetryFailureRouter.Resolve(_settings, null, exception);

        result.Topic.Should().Be(_settings.RetryTiers[0].Topic);
        result.Delay.Should().Be(TimeSpan.FromSeconds(5));
        result.Attempt.Should().Be(1);
        result.IsDeadLetter.Should().BeFalse();
    }

    [Fact]
    public void Resolve_TransientFailureOnRetryTier_SelectsNextTier()
    {
        var result = RetryFailureRouter.Resolve(_settings, 1, new TransientException("failure"));

        result.Topic.Should().Be(_settings.RetryTiers[2].Topic);
        result.Delay.Should().Be(TimeSpan.FromSeconds(60));
        result.Attempt.Should().Be(3);
        result.IsDeadLetter.Should().BeFalse();
    }

    [Theory]
    [InlineData(null, typeof(NonTransientException))]
    [InlineData(null, typeof(InvalidOperationException))]
    [InlineData(0, typeof(IsolableException))]
    [InlineData(0, typeof(NonTransientException))]
    [InlineData(3, typeof(TransientException))]
    public void Resolve_NonRetryableOrExhaustedFailure_SelectsDlq(int? sourceTier, Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "failure")!;

        var result = RetryFailureRouter.Resolve(_settings, sourceTier, exception);

        result.Topic.Should().Be(_settings.DeadLetterTopic);
        result.IsDeadLetter.Should().BeTrue();
        result.Delay.Should().BeNull();
        result.Attempt.Should().BeNull();
    }

}
