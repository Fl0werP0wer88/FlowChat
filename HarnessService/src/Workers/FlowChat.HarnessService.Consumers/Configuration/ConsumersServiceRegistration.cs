using FlowChat.HarnessService.Application;
using FlowChat.HarnessService.Application.Contracts.Persistence;
using FlowChat.HarnessService.Application.Features.Projections.Commands.BulkUpsert;
using FlowChat.HarnessService.Consumers.Configuration.Settings;
using FlowChat.HarnessService.Consumers.Kafka.Projections;
using FlowChat.HarnessService.Consumers.Projections.Models;
using FlowChat.HarnessService.Infrastructure;
using FlowChat.HarnessService.Persistence;
using FlowChat.HarnessService.Persistence.BulkUpsert.Projections;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

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

        services.AddApplicationServices();
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
                        ProjectionCommandItem,
                        IProjectionTestBulkRepository,
                        ProjectionTestBulkRepository>()
                    .AddCommandHandler<
                        ProjectionCommandItem,
                        IProjectionTestBulkRepository>()
                    .AddConsumer<
                        AppDbContext,
                        ProjectionTestReadModel,
                        ProjectionCommandItem,
                        ProjectionTestCommandItemFactory>());

        return services;
    }
}
