using Confluent.Kafka;
using FlowChat.Core.Contracts;
using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Services;
using FlowChat.RealtimeService.Routing;
using FlowChat.RealtimeService.Routing.Configuration;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Redis;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using StackExchange.Redis;

namespace FlowChat.RealtimeService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new SettingsProvider(configuration));

        var settingsProvider = new SettingsProvider(configuration);
        services.TryAddSingleton(sp => sp.GetRequiredService<ISettingsProvider>().GetSection<RealtimeApiSettingsSection>());
        services.TryAddSingleton(sp => sp.GetRequiredService<ISettingsProvider>().GetSection<RealtimeRoutingSettingsSection>());

        var chatMessageSentConsumerOptions = settingsProvider.GetSection<ChatMessageSentConsumerSettingsSection>();
        var presenceStatusChangedConsumerOptions = settingsProvider.GetSection<PresenceStatusChangedConsumerSettingsSection>();

        services.AddHttpClient(RealtimeInternalApiClient.HttpClientName, (serviceProvider, httpClient) =>
        {
            var realtimeApiSettings = serviceProvider
                .GetRequiredService<ISettingsProvider>()
                .GetSection<RealtimeApiSettingsSection>();
            httpClient.DefaultRequestHeaders.Remove(RealtimeInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(realtimeApiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(RealtimeInternalApiClient.ApiKeyHeaderName, realtimeApiSettings.ApiKey);
            }
        });
        services.AddScoped<IRealtimeInternalApiClient>(serviceProvider =>
            new RealtimeInternalApiClient(
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(RealtimeInternalApiClient.HttpClientName)));
        services.TryAddSingleton<IRealtimeInstanceAddressResolver, ConfiguredRealtimeInstanceAddressResolver>();
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var options = ConfigurationOptions.Parse(sp.GetRequiredService<RealtimeRoutingSettingsSection>().RedisConnectionString);
            options.AbortOnConnectFail = false;

            return ConnectionMultiplexer.Connect(options);
        });
        services.TryAddSingleton<RedisUnitOfWork>();
        services.TryAddSingleton<IRedisTransactionContext>(sp => sp.GetRequiredService<RedisUnitOfWork>());
        services.TryAddSingleton<IRealtimeRoutingTopologyReader, RedisRealtimeRoutingTopologyStore>();
        services.AddScoped<IRealtimeEventRouter, RealtimeEventRouter>();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(chatMessageSentConsumerOptions, presenceStatusChangedConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(chatMessageSentConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(chatMessageSentConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(presenceStatusChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(presenceStatusChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(presenceStatusChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(presenceStatusChangedConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(presenceStatusChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(presenceStatusChangedConsumerOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(chatMessageSentConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(presenceStatusChangedConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(presenceStatusChangedConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ChatMessageSentSubscriber>()
            .AddScopedSubscriber<UserPresenceChangedSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        ChatMessageSentConsumerSettingsSection chatMessageSentConsumerOptions,
        PresenceStatusChangedConsumerSettingsSection presenceStatusChangedConsumerOptions) =>
        !string.IsNullOrWhiteSpace(chatMessageSentConsumerOptions.BootstrapServers)
            ? chatMessageSentConsumerOptions.BootstrapServers
            : presenceStatusChangedConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
