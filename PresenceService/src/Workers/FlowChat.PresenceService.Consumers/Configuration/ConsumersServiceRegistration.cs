using FlowChat.Core.Messaging.ChatService.ReadModels;
using FlowChat.PresenceService.Application;
using FlowChat.PresenceService.Application.Features.ContactObserverProjections;
using FlowChat.PresenceService.Consumers.Configuration.Settings;
using FlowChat.PresenceService.Consumers.Kafka.Projections;
using FlowChat.PresenceService.Infrastructure;
using FlowChat.PresenceService.Persistence;
using FlowChat.PresenceService.Persistence.BulkUpsert.Projections;
using FlowChat.PresenceService.Persistence.Entities;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.PresenceService.Consumers;

public static class ConsumersServiceRegistration
{
    internal const string ContactMainConsumerName = "contact-main";
    internal const string ContactRetryConsumerName = "contact-retry";

    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var contactOptions = configuration
            .GetSection(new DuetConversationContactConsumerSettingsSection().SectionName)
            .Get<DuetConversationContactConsumerSettingsSection>()
            ?? new DuetConversationContactConsumerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices(configuration);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddProjectionBulk(
                contactOptions,
                ContactMainConsumerName,
                ContactRetryConsumerName,
                bulkBuilder => bulkBuilder
                    .AddRepository<
                        AppDbContext,
                        ContactObserverProjectionDto,
                        ContactObserverReadModelEntity,
                        ContactObserverProjectionBulkEntityFactory>()
                    .AddCommandHandler<ContactObserverProjectionDto>()
                    .AddConsumer<
                        AppDbContext,
                        DuetConversationContactStateReadModel,
                        ContactObserverProjectionDto,
                        (Guid, Guid),
                        ContactObserverProjectionValueFactory>());

        return services;
    }
}
