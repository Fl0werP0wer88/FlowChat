using Confluent.Kafka;
using FlowChat.Core.Exceptions;
using FlowChat.NotificationService.Consumers.Configuration;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;

namespace FlowChat.NotificationService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(UserEmailVerificationRequestedConsumerOptions.SectionName)
            .Get<UserEmailVerificationRequestedConsumerOptions>()
            ?? new UserEmailVerificationRequestedConsumerOptions();

        services.AddOptions<NotificationApiSettings>()
            .BindConfiguration(NotificationApiSettings.SectionName);
        services.AddHttpClient<INotificationInternalApiClient, NotificationInternalApiClient>();

        services.AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(consumerOptions.BootstrapServers)
                    .AddConsumer(consumer => consumer
                        .WithGroupId(consumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureMainEndpoint(endpoint, consumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(consumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => ConfigureRetryEndpoint(endpoint, consumerOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(consumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(consumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<UserEmailVerificationRequestedSubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureMainEndpoint(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        UserEmailVerificationRequestedConsumerOptions consumerOptions) =>
        ConfigureEndpointDefaults(endpoint, consumerOptions.Topic)
            .OnError(policy =>
            {
                policy.MoveTo(consumerOptions.DeadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.MoveTo(consumerOptions.RetryTopic, move => move
                    .Exclude<NonTransientException>());
            });

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureRetryEndpoint(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        UserEmailVerificationRequestedConsumerOptions consumerOptions) =>
        ConfigureEndpointDefaults(endpoint, consumerOptions.RetryTopic)
            .OnError(policy =>
            {
                policy.MoveTo(consumerOptions.DeadLetterTopic, move => move
                    .ApplyTo<NonTransientException>());

                policy.Retry(retry => retry
                        .WithMaxRetries(consumerOptions.MaxRetryCount)
                        .Exclude<NonTransientException>()
                        .WithExponentialDelay(
                            TimeSpan.FromSeconds(consumerOptions.RetryBaseDelaySeconds),
                            2,
                            TimeSpan.FromSeconds(consumerOptions.RetryMaxDelaySeconds)))
                    .ThenMoveTo(consumerOptions.DeadLetterTopic, move => move
                        .Exclude<NonTransientException>());
            });

    private static KafkaConsumerEndpointConfigurationBuilder<object> ConfigureEndpointDefaults(
        KafkaConsumerEndpointConfigurationBuilder<object> endpoint,
        string topic) =>
        endpoint
            .ConsumeFrom(topic)
            .DeserializeJson(deserializer => deserializer.WithOptionalMessageTypeHeader())
            .IgnoreUnhandledMessages();
}
