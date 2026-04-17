using Confluent.Kafka;
using FlowChat.PresenceService.Consumers.Configuration;
using FlowChat.PresenceService.Consumers.Kafka;
using FlowChat.PresenceService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.PresenceService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var contactOptions = configuration
            .GetSection(SocialGraphContactConsumerSettingsSection.SectionName)
            .Get<SocialGraphContactConsumerSettingsSection>()
            ?? new SocialGraphContactConsumerSettingsSection();
        var realtimeConnectionOptions = configuration
            .GetSection(RealtimeConnectionConsumerSettingsSection.SectionName)
            .Get<RealtimeConnectionConsumerSettingsSection>()
            ?? new RealtimeConnectionConsumerSettingsSection();

        services.AddOptions<PresenceApiSettingsSection>()
            .BindConfiguration(PresenceApiSettingsSection.SectionName);
        services.AddHttpClient(PresenceInternalApiClient.HttpClientName, (serviceProvider, httpClient) =>
        {
            var apiSettings = serviceProvider.GetRequiredService<IOptions<PresenceApiSettingsSection>>().Value;
            if (!Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("PresenceApi:BaseUrl must be an absolute URI.");
            }

            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.Remove(PresenceInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(PresenceInternalApiClient.ApiKeyHeaderName, apiSettings.ApiKey);
            }
        });
        services.AddScoped<IPresenceInternalApiClient>(serviceProvider =>
            new PresenceInternalApiClient(
                serviceProvider.GetRequiredService<IHttpClientFactory>()
                    .CreateClient(PresenceInternalApiClient.HttpClientName)));

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(contactOptions, realtimeConnectionOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(contactOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(contactOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(contactOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(contactOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(contactOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(contactOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(realtimeConnectionOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(realtimeConnectionOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(realtimeConnectionOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(realtimeConnectionOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(realtimeConnectionOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(realtimeConnectionOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(contactOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(contactOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(realtimeConnectionOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(realtimeConnectionOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ContactAddedSubscriber>()
            .AddScopedSubscriber<ContactDeletedSubscriber>()
            .AddScopedSubscriber<RealtimeConnectionRegisteredSubscriber>()
            .AddScopedSubscriber<RealtimeConnectionUnregisteredSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        SocialGraphContactConsumerSettingsSection contactOptions,
        RealtimeConnectionConsumerSettingsSection realtimeConnectionOptions) =>
        !string.IsNullOrWhiteSpace(contactOptions.BootstrapServers)
            ? contactOptions.BootstrapServers
            : realtimeConnectionOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
