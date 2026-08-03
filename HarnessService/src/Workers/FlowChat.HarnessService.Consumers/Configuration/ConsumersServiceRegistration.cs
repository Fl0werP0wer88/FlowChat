using FlowChat.HarnessService.Application;
using FlowChat.HarnessService.Application.Features.Projections;
using FlowChat.HarnessService.Consumers.Configuration.Settings;
using FlowChat.HarnessService.Consumers.Kafka.Projections;
using FlowChat.HarnessService.Consumers.Kafka.Retry;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Persistence.Entities.Projections;
using FlowChat.HarnessService.Infrastructure;
using FlowChat.HarnessService.Persistence;
using FlowChat.HarnessService.Persistence.BulkUpsert.Projections;
using FlowChat.Shared.Consumers.Projections.Bulk;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.HarnessService.Consumers;

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
        services.AddConsumerApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices();

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddProjectionBulk(
                projectionOptions,
                ProjectionMainConsumerName,
                ProjectionRetryConsumerName,
                bulkBuilder => bulkBuilder
                    .AddRepository<
                        AppDbContext,
                        ProjectionTestDto,
                        ProjectionTestEntity,
                        ProjectionTestBulkEntityFactory>()
                    .AddCommandHandler<ProjectionTestDto>()
                    .AddConsumer<
                        AppDbContext,
                        ProjectionTestReadModel,
                        ProjectionTestDto,
                        Guid,
                        ProjectionTestValueFactory>());

        return services;
    }

    public static IServiceCollection AddTieredRetryHarnessConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var retryPipelineOptions = configuration
            .GetSection(new RetryPipelineConsumerSettingsSection().SectionName)
            .Get<RetryPipelineConsumerSettingsSection>()
            ?? new RetryPipelineConsumerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices();
        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddFlowChatTieredRetryConsumerPipeline<AppDbContext>(
                retryPipelineOptions.BootstrapServers,
                [retryPipelineOptions])
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddScopedSubscriber<RetryPipelineTestSubscriber>();

        return services;
    }
}
