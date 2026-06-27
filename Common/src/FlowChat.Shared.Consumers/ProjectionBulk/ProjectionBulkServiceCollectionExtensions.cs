using Confluent.Kafka;
using FlowChat.Core.Messaging;
using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.Shared.Consumers.ProjectionBulk;

public static class ProjectionBulkServiceCollectionExtensions
{
    public static SilverbackBuilder AddProjectionBulkRepository<TItem, TRepository, TImplementation>(
        this SilverbackBuilder builder)
        where TItem : notnull
        where TRepository : class, IProjectionBulkRepository<TItem>
        where TImplementation : class, TRepository
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<TRepository, TImplementation>();

        return builder;
    }

    public static SilverbackBuilder AddProjectionBulkCommandHandler<TItem, TRepository>(
        this SilverbackBuilder builder)
        where TItem : notnull
        where TRepository : class, IProjectionBulkRepository<TItem>
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddScoped<IProjectionOffsetStore, SilverbackProjectionOffsetStore>();
        builder.Services.AddScoped<
            IRequestHandler<ProjectionBulkCommand<TItem>, FlowChatResult<Unit>>,
            ProjectionBulkCommandHandlerBaseV2<
                ProjectionBulkCommand<TItem>,
                TItem,
                TRepository>>();

        return builder;
    }

    public static SilverbackBuilder AddProjectionBulkConsumer<TDbContext, TReadModel, TItem, TItemFactory, TBatchSubscriber, TRetrySubscriber>(
        this SilverbackBuilder builder,
        IProjectionBulkConsumerSettingsSection options,
        string mainConsumerName,
        string retryConsumerName)
        where TDbContext : DbContext
        where TReadModel : class
        where TItem : notnull
        where TItemFactory : class, IProjectionCommandItemFactory<TReadModel, TItem>
        where TBatchSubscriber : class
        where TRetrySubscriber : class
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(mainConsumerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(retryConsumerName);

        builder.Services.AddScoped<IProjectionCommandItemFactory<TReadModel, TItem>, TItemFactory>();

        return builder
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
            .AddScopedSubscriber<TBatchSubscriber>()
            .AddScopedSubscriber<TRetrySubscriber>();
    }

    private static AutoOffsetReset ParseAutoOffsetReset(string value) =>
        Enum.TryParse<AutoOffsetReset>(value, true, out var parsed)
            ? parsed
            : AutoOffsetReset.Earliest;
}
