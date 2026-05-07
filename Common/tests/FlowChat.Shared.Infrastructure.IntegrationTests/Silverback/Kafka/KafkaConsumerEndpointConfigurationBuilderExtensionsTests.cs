using System.Reflection;
using Confluent.Kafka;
using FlowChat.Core.Exceptions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace FlowChat.Shared.Infrastructure.IntegrationTests.Silverback.Kafka;

public sealed class KafkaConsumerEndpointConfigurationBuilderExtensionsTests
{
    [Fact]
    public async Task ConfigureFlowChatEndpointDefaults_WithTopic_ConfiguresTopicDeserializerAndUnhandledMessages()
    {
        var configuration = await GetEndpointConfigurationAsync(endpoint =>
            endpoint.ConfigureFlowChatEndpointDefaults("user-profile-events"));

        configuration.TopicPartitions.Should().ContainSingle(topicPartition =>
            topicPartition.Topic == "user-profile-events" &&
            topicPartition.Partition == Partition.Any);
        configuration.ThrowIfUnhandled.Should().BeFalse();

        var deserializer = configuration.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>().Subject;
        deserializer.TypeHeaderBehavior.Should().Be(JsonMessageDeserializerTypeHeaderBehavior.Optional);
    }

    [Fact]
    public async Task ConfigureFlowChatMainEndpoint_WithOptions_ConfiguresDeadLetterAndRetryMovePolicies()
    {
        var options = new TestRetryableKafkaConsumerOptions
        {
            Topic = "user-profile-events",
            RetryTopic = "user-profile-events-retry",
            DeadLetterTopic = "user-profile-events-dead-letter"
        };

        var configuration = await GetEndpointConfigurationAsync(endpoint =>
            endpoint.ConfigureFlowChatMainEndpoint(options));

        configuration.TopicPartitions.Should().ContainSingle(topicPartition =>
            topicPartition.Topic == options.Topic &&
            topicPartition.Partition == Partition.Any);

        var policies = GetPolicies(configuration.ErrorPolicy);

        policies.Should().HaveCount(2);

        var deadLetterPolicy = policies[0].Should().BeOfType<MoveMessageErrorPolicy>().Subject;
        deadLetterPolicy.EndpointName.Should().Be(options.DeadLetterTopic);
        deadLetterPolicy.ExcludedExceptions.Should().ContainSingle()
            .Which.Should().Be(typeof(TransientException));

        var retryPolicy = policies[1].Should().BeOfType<MoveMessageErrorPolicy>().Subject;
        retryPolicy.EndpointName.Should().Be(options.RetryTopic);
        retryPolicy.IncludedExceptions.Should().ContainSingle()
            .Which.Should().Be(typeof(TransientException));
    }

    [Fact]
    public async Task ConfigureFlowChatRetryEndpoint_WithOptions_ConfiguresDeadLetterRetryAndFallbackPolicies()
    {
        var options = new TestRetryableKafkaConsumerOptions
        {
            RetryTopic = "chat-events-retry",
            DeadLetterTopic = "chat-events-dead-letter",
            MaxRetryCount = 5,
            RetryBaseDelaySeconds = 3,
            RetryMaxDelaySeconds = 30
        };

        var configuration = await GetEndpointConfigurationAsync(endpoint =>
            endpoint.ConfigureFlowChatRetryEndpoint(options));

        configuration.TopicPartitions.Should().ContainSingle(topicPartition =>
            topicPartition.Topic == options.RetryTopic &&
            topicPartition.Partition == Partition.Any);

        var policies = GetPolicies(configuration.ErrorPolicy);

        policies.Should().HaveCount(3);

        var deadLetterForNonTransient = policies[0].Should().BeOfType<MoveMessageErrorPolicy>().Subject;
        deadLetterForNonTransient.EndpointName.Should().Be(options.DeadLetterTopic);
        deadLetterForNonTransient.ExcludedExceptions.Should().ContainSingle()
            .Which.Should().Be(typeof(TransientException));

        var retryPolicy = policies[1].Should().BeOfType<RetryErrorPolicy>().Subject;
        retryPolicy.MaxFailedAttempts.Should().Be(options.MaxRetryCount);
        retryPolicy.InitialDelay.Should().Be(TimeSpan.FromSeconds(options.RetryBaseDelaySeconds));
        retryPolicy.DelayFactor.Should().Be(2);
        retryPolicy.MaxDelay.Should().Be(TimeSpan.FromSeconds(options.RetryMaxDelaySeconds));
        retryPolicy.IncludedExceptions.Should().ContainSingle()
            .Which.Should().Be(typeof(TransientException));

        var deadLetterFallback = policies[2].Should().BeOfType<MoveMessageErrorPolicy>().Subject;
        deadLetterFallback.EndpointName.Should().Be(options.DeadLetterTopic);
        deadLetterFallback.IncludedExceptions.Should().ContainSingle()
            .Which.Should().Be(typeof(TransientException));
    }

    private static async Task<KafkaConsumerEndpointConfiguration> GetEndpointConfigurationAsync(
        Func<KafkaConsumerEndpointConfigurationBuilder<object>, KafkaConsumerEndpointConfigurationBuilder<object>> configureEndpoint)
    {
        await using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new KafkaConsumerConfigurationBuilder(serviceProvider)
            .WithBootstrapServers("localhost:9092")
            .WithGroupId("test-group")
            .Consume(endpoint => configureEndpoint(endpoint));

        var configuration = builder.Build();

        return configuration.Endpoints.Should().ContainSingle()
            .Which.Should().BeOfType<KafkaConsumerEndpointConfiguration>().Subject;
    }

    private static IReadOnlyList<ErrorPolicyBase> GetPolicies(IErrorPolicy errorPolicy)
    {
        errorPolicy.Should().BeOfType<ErrorPolicyChain>();

        var policiesField = errorPolicy
            .GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .FirstOrDefault(field => typeof(IEnumerable<ErrorPolicyBase>).IsAssignableFrom(field.FieldType));

        policiesField.Should().NotBeNull();

        return ((IEnumerable<ErrorPolicyBase>)policiesField!.GetValue(errorPolicy)!).ToArray();
    }

    private sealed class TestRetryableKafkaConsumerOptions : IRetryableKafkaConsumerSettingsSection
    {
        public string BootstrapServers { get; init; } = "localhost:9092";
        public string Topic { get; init; } = "test-topic";
        public string GroupId { get; init; } = "test-group";
        public string AutoOffsetReset { get; init; } = "Earliest";
        public string RetryTopic { get; init; } = "test-topic-retry";
        public string RetryGroupId { get; init; } = "test-group-retry";
        public string DeadLetterTopic { get; init; } = "test-topic-dead-letter";
        public int MaxRetryCount { get; init; } = 3;
        public int RetryBaseDelaySeconds { get; init; } = 5;
        public int RetryMaxDelaySeconds { get; init; } = 60;
    }
}
