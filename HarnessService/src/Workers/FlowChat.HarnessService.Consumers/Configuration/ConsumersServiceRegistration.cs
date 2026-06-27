using Confluent.Kafka;
using FlowChat.Core.Messaging;
using FlowChat.HarnessService.Application;
using FlowChat.HarnessService.Consumers.Configuration.Settings;
using FlowChat.HarnessService.Consumers.Kafka.Projections;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Infrastructure;
using FlowChat.HarnessService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.HarnessService.Consumers;

// Registers the DLQ producer so MoveMessageErrorPolicy can find it by topic name,
// without exposing it to IPublisher routing for ProjectionIntegrationEvent<T>.
file sealed record DlqSentinel;

public static class ConsumersServiceRegistration
{
    internal const string ProjectionMainConsumerName = "projection-main";
    internal const string ProjectionRetryConsumerName = "projection-retry";

    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var projectionOptions = configuration
            .GetSection(new ProjectionConsumerSettingsSection().SectionName)
            .Get<ProjectionConsumerSettingsSection>()
            ?? new ProjectionConsumerSettingsSection();

        services.AddApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(projectionOptions.BootstrapServers)
                    .AddConsumer(ProjectionMainConsumerName, consumer => consumer
                        .WithGroupId(projectionOptions.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(projectionOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatMainEndpoint(projectionOptions)
                            .EnableBatchProcessing(
                                projectionOptions.BatchSize,
                                TimeSpan.FromMilliseconds(projectionOptions.BatchMaxWaitTimeMilliseconds))))
                    .AddConsumer(ProjectionRetryConsumerName, consumer => consumer
                        .WithGroupId(projectionOptions.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(projectionOptions.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<AppDbContext>())
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatRetryEndpoint(projectionOptions)))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<ProjectionTestReadModel>>(endpoint => endpoint
                            .ProduceTo(projectionOptions.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<DlqSentinel>(endpoint => endpoint
                            .ProduceTo(projectionOptions.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ProjectionBatchSubscriber>()
            .AddScopedSubscriber<ProjectionRetrySubscriber>();

        return services;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
