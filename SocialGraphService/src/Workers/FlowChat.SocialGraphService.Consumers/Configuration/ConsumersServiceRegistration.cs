using Confluent.Kafka;
using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Consumers.Configuration.Settings;
using FlowChat.SocialGraphService.Consumers.Kafka;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.SocialGraphService.Consumers;

public static class ConsumersServiceRegistration
{
    internal const string UserProfileMainConsumerName = "user-profile-main";
    internal const string UserProfileRetryConsumerName = "user-profile-retry";

    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumersAssembly = typeof(ConsumersServiceRegistration).Assembly;
        var consumerOptions = configuration
            .GetSection(new UserProfileConsumerSettingsSection().SectionName)
            .Get<UserProfileConsumerSettingsSection>()
            ?? new UserProfileConsumerSettingsSection();

        services.AddWorkerApplicationServices();
        services.AddPersistenceServices(configuration);
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, consumersAssembly);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options.AddKafka())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(consumerOptions.BootstrapServers)
                    .AddConsumer(UserProfileMainConsumerName, consumer => consumer
                        .WithGroupId(consumerOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatMainEndpoint(consumerOptions)
                            .EnableBatchProcessing(
                                consumerOptions.BatchSize,
                                TimeSpan.FromMilliseconds(consumerOptions.BatchMaxWaitTimeMilliseconds))))
                    .AddConsumer(UserProfileRetryConsumerName, consumer => consumer
                        .WithGroupId(consumerOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(consumerOptions.AutoOffsetReset))
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatRetryEndpoint(consumerOptions)))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(consumerOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint
                            .ProduceTo(consumerOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<UserProfileProjectionBatchSubscriber>()
            .AddScopedSubscriber<UserProfileProjectionRetrySubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}


