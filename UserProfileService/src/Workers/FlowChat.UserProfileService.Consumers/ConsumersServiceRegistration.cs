using Confluent.Kafka;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.UserProfileService.Consumers.Configuration;
using FlowChat.UserProfileService.Consumers.Kafka;
using FlowChat.UserProfileService.Consumers.Services;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.UserProfileService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(AccountRegisteredConsumerOptions.SectionName)
            .Get<AccountRegisteredConsumerOptions>()
            ?? new AccountRegisteredConsumerOptions();

        services.AddOptions<UserProfileApiSettings>()
            .BindConfiguration(UserProfileApiSettings.SectionName);
        services.AddHttpClient(UserProfileInternalApiClient.HttpClientName, (serviceProvider, httpClient) =>
        {
            var apiSettings = serviceProvider.GetRequiredService<IOptions<UserProfileApiSettings>>().Value;
            if (!Uri.TryCreate(apiSettings.BaseUrl, UriKind.Absolute, out var baseAddress))
            {
                throw new InvalidOperationException("UserProfileApi:BaseUrl must be an absolute URI.");
            }

            httpClient.BaseAddress = baseAddress;
            httpClient.DefaultRequestHeaders.Remove(UserProfileInternalApiClient.ApiKeyHeaderName);

            if (!string.IsNullOrWhiteSpace(apiSettings.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add(UserProfileInternalApiClient.ApiKeyHeaderName, apiSettings.ApiKey);
            }
        });
        services.AddScoped<IUserProfileInternalApiClient>(serviceProvider =>
            new UserProfileInternalApiClient(
                serviceProvider
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(UserProfileInternalApiClient.HttpClientName)));

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesBehavior>()
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
            .AddScopedSubscriber<AccountRegisteredSubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}


