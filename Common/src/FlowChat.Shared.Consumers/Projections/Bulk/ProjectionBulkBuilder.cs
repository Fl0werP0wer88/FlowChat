using Confluent.Kafka;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Persistance.ProjectionBulk;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.Subscriptions;

namespace FlowChat.Shared.Consumers.Projections.Bulk;

public sealed class ProjectionBulkBuilder(
    SilverbackBuilder silverbackBuilder,
    IProjectionBulkConsumerSettingsSection options,
    string mainConsumerName,
    string retryConsumerName)
{
    public ProjectionBulkBuilder AddRepository<TDbContext, TValue, TEntity, TEntityFactory>()
        where TDbContext : DbContext
        where TValue : class
        where TEntity : ReadModelEntityBase
        where TEntityFactory : class, IProjectionBulkEntityFactory<TValue, TEntity>
    {
        silverbackBuilder.Services.AddScoped<TEntityFactory>();
        silverbackBuilder.Services.AddScoped<
            IProjectionBulkEntityFactory<TValue, TEntity>,
            TEntityFactory>();
        silverbackBuilder.Services.AddScoped<
            IProjectionBulkRepository<ProjectionCommandItem<TValue>>,
            ProjectionBulkRepository<TDbContext, TValue, TEntity, TEntityFactory>>();

        return this;
    }

    public ProjectionBulkBuilder AddCommandHandler<TValue>()
        where TValue : class
    {
        silverbackBuilder.Services.AddScoped<
            IRequestHandler<ProjectionBulkCommand<ProjectionCommandItem<TValue>>, FlowChatResult<Unit>>,
            ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<ProjectionCommandItem<TValue>>,
                ProjectionCommandItem<TValue>,
                IProjectionBulkRepository<ProjectionCommandItem<TValue>>>>();

        return this;
    }

    public ProjectionBulkBuilder AddConsumer<TDbContext, TReadModel, TValue, TKey, TValueFactory>()
        where TDbContext : DbContext
        where TReadModel : class
        where TValue : class
        where TKey : notnull
        where TValueFactory : class, IProjectionValueFactory<TReadModel, TValue, TKey>
    {
        silverbackBuilder.Services.AddScoped<IProjectionValueFactory<TReadModel, TValue, TKey>, TValueFactory>();

        silverbackBuilder
            .WithConnectionToMessageBroker(broker => broker
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore())
            .AddKafkaClients(clients =>
            {
                clients
                    .WithBootstrapServers(options.BootstrapServers)
                    .AddConsumer(mainConsumerName, consumer => consumer
                        .WithGroupId(options.GroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(options.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<TDbContext>())
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatMainEndpoint(options)
                            .EnableBatchProcessing(
                                options.BatchSize,
                                TimeSpan.FromMilliseconds(options.BatchMaxWaitTimeMilliseconds))))
                    .AddConsumer(retryConsumerName, consumer => consumer
                        .WithGroupId(options.RetryGroupId)
                        .WithAutoOffsetReset(ParseAutoOffsetReset(options.AutoOffsetReset))
                        .StoreOffsetsClientSide(store => store.UseEntityFramework<TDbContext>())
                        .Consume(endpoint => endpoint
                            .ConfigureFlowChatRetryEndpoint(options)))
                    .AddProducer(producer => producer
                        .Produce<ProjectionIntegrationEvent<TReadModel>>(endpoint => endpoint
                            .ProduceTo(options.RetryTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())))
                    .AddProducer(producer => producer
                        .Produce<ProjectionBulkDeadLetterSentinel>(endpoint => endpoint
                            .ProduceTo(options.DeadLetterTopic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
            })
            .AddScopedSubscriber<ProjectionBatchSubscriber<TReadModel, TValue, TKey>>(
                new TypeSubscriptionOptions
                {
                    Filters = [new ConsumerNameFilterAttribute(mainConsumerName)]
                })
            .AddScopedSubscriber<ProjectionRetrySubscriber<TReadModel, TValue, TKey>>(
                new TypeSubscriptionOptions
                {
                    Filters = [new ConsumerNameFilterAttribute(retryConsumerName)]
                });

        return this;
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
