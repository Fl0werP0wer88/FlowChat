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

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public sealed class ProjectionBulkBuilder(
    SilverbackBuilder silverbackBuilder,
    IProjectionBulkConsumerSettingsSection options,
    string mainConsumerName,
    string retryConsumerName)
{
    public ProjectionBulkBuilder AddRepository<TDbContext, TItem, TValue, TEntity, TEntityFactory>()
        where TDbContext : DbContext
        where TItem : notnull, IProjectionCommandItem<TValue>
        where TValue : class
        where TEntity : ReadModelEntityBase
        where TEntityFactory : class, IProjectionBulkEntityFactory<TItem, TValue, TEntity>
    {
        silverbackBuilder.Services.AddScoped<TEntityFactory>();
        silverbackBuilder.Services.AddScoped<
            IProjectionBulkEntityFactory<TItem, TValue, TEntity>,
            TEntityFactory>();
        silverbackBuilder.Services.AddScoped<
            IProjectionBulkRepository<TItem>,
            ProjectionBulkRepository<TDbContext, TItem, TValue, TEntity, TEntityFactory>>();

        return this;
    }

    public ProjectionBulkBuilder AddCommandHandler<TItem>()
        where TItem : notnull
    {
        silverbackBuilder.Services.AddScoped<IProjectionOffsetStore, SilverbackProjectionOffsetStore>();
        silverbackBuilder.Services.AddScoped<
            IRequestHandler<ProjectionBulkCommand<TItem>, FlowChatResult<Unit>>,
            ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<TItem>,
                TItem,
                IProjectionBulkRepository<TItem>>>();

        return this;
    }

    public ProjectionBulkBuilder AddConsumer<TDbContext, TReadModel, TItem, TItemFactory>()
        where TDbContext : DbContext
        where TReadModel : class
        where TItem : notnull
        where TItemFactory : class, IProjectionCommandItemFactory<TReadModel, TItem>
    {
        silverbackBuilder.Services.AddScoped<IProjectionCommandItemFactory<TReadModel, TItem>, TItemFactory>();

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
            .AddScopedSubscriber<ProjectionBatchSubscriber<TReadModel, TItem>>(
                new TypeSubscriptionOptions
                {
                    Filters = [new ConsumerNameFilterAttribute(mainConsumerName)]
                })
            .AddScopedSubscriber<ProjectionRetrySubscriber<TReadModel, TItem>>(
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
