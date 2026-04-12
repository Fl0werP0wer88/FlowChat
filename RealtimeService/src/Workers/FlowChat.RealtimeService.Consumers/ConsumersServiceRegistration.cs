using Confluent.Kafka;
using FlowChat.RealtimeService.Consumers.Configuration;
using FlowChat.RealtimeService.Consumers.Kafka;
using FlowChat.RealtimeService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.RealtimeService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settingsManager = new ConsumersSettingsManager(configuration);
        services.TryAddSingleton<IConsumersSettingsManager>(settingsManager);

        var chatMessageSentConsumerOptions = settingsManager.GetChatMessageSentConsumerOptions();
        var userPresenceChangedConsumerOptions = settingsManager.GetUserStatusChangedConsumerOptions();

        services.AddHttpClient(RealtimeInternalApiClient.HttpClientName, (serviceProvider, httpClient) =>
        {
            var realtimeApiSettings = serviceProvider
                .GetRequiredService<IConsumersSettingsManager>()
                .GetRealtimeApiSettings();
            if (!Uri.TryCreate(realtimeApiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("RealtimeApi:BaseUrl must be an absolute URI.");
            }

            httpClient.BaseAddress = baseAddress;
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

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(ResolveBootstrapServers(chatMessageSentConsumerOptions, userPresenceChangedConsumerOptions))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(chatMessageSentConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(chatMessageSentConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(chatMessageSentConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(chatMessageSentConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userPresenceChangedConsumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userPresenceChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatMainEndpoint(userPresenceChangedConsumerOptions)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(userPresenceChangedConsumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(userPresenceChangedConsumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint.ConfigureFlowChatRetryEndpoint(userPresenceChangedConsumerOptions)))
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
                            .ProduceTo(userPresenceChangedConsumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(userPresenceChangedConsumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ChatMessageSentSubscriber>()
            .AddScopedSubscriber<UserPresenceChangedSubscriber>();

        return services;
    }

    private static string ResolveBootstrapServers(
        ChatMessageSentConsumerOptions chatMessageSentConsumerOptions,
        UserPresenceChangedConsumerOptions userPresenceChangedConsumerOptions) =>
        !string.IsNullOrWhiteSpace(chatMessageSentConsumerOptions.BootstrapServers)
            ? chatMessageSentConsumerOptions.BootstrapServers
            : userPresenceChangedConsumerOptions.BootstrapServers;

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}


