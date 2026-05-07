using Confluent.Kafka;
using FlowChat.NotificationService.Consumers.Configuration.Settings;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Shared.Infrastructure.Http;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.NotificationService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedConsumerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedConsumerSettingsSection>()
            ?? new UserEmailVerificationRequestedConsumerSettingsSection();

        services.AddOptions<NotificationApiSettingsSection>()
            .BindConfiguration(new NotificationApiSettingsSection().SectionName);
        services.AddFlowChatHttpClient<INotificationInternalApiClient, NotificationInternalApiClient>((serviceProvider, httpClient) =>
        {
            var apiSettings = serviceProvider.GetRequiredService<IOptions<NotificationApiSettingsSection>>().Value;
            if (!Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("NotificationApi:BaseUrl must be an absolute URI.");
            }

            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.Remove(NotificationInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(NotificationInternalApiClient.ApiKeyHeaderName, apiSettings.ApiKey);
            }
        });

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(consumerOptions.BootstrapServers)
                    .AddConsumer(consumer => consumer
                        .WithGroupId(consumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(consumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(consumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(consumerOptions)))
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
}


