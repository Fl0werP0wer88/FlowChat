using Confluent.Kafka;
using FlowChat.AuthService.Consumers.Configuration.Settings;
using FlowChat.AuthService.Consumers.Kafka;
using FlowChat.AuthService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.AuthService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(new UserEmailConfirmedConsumerSettingsSection().SectionName)
            .Get<UserEmailConfirmedConsumerSettingsSection>()
            ?? new UserEmailConfirmedConsumerSettingsSection();

        services.AddOptions<AuthApiSettingsSection>()
            .BindConfiguration(new AuthApiSettingsSection().SectionName);
        services.AddHttpClient(AuthInternalApiClient.HttpClientName, (serviceProvider, httpClient) =>
        {
            var apiSettings = serviceProvider.GetRequiredService<IOptions<AuthApiSettingsSection>>().Value;
            if (!Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("AuthApi:BaseUrl must be an absolute URI.");
            }

            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.Remove(AuthInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(AuthInternalApiClient.ApiKeyHeaderName, apiSettings.ApiKey);
            }
        });
        services.AddScoped<IAuthInternalApiClient>(serviceProvider =>
            new AuthInternalApiClient(
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(AuthInternalApiClient.HttpClientName)));

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
            .AddScopedSubscriber<UserEmailConfirmedSubscriber>()
            .AddScopedSubscriber<AuthEmailChangedSubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
