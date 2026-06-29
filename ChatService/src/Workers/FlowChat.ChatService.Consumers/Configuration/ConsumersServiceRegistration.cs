using FlowChat.ChatService.Application;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Consumers.Configuration.Settings;
using FlowChat.ChatService.Consumers.Kafka.Projections;
using FlowChat.ChatService.Infrastructure;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.BulkUpsert.Projections;
using FlowChat.ChatService.Persistence.Entities;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace FlowChat.ChatService.Consumers;

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

        services.AddConsumerApplicationServices();
        services.AddConsumerPersistenceServices(configuration);
        services.AddConsumerInfrastructureServices(configuration);
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, consumersAssembly);

        services.AddSilverback()
            .AddSingletonBrokerBehavior<CustomSpanAttributesProducerBehavior>()
            .AddSingletonBrokerBehavior<CustomSpanAttributesConsumerBehavior>()
            .AddProjectionBulk(
                consumerOptions,
                UserProfileMainConsumerName,
                UserProfileRetryConsumerName,
                bulkBuilder => bulkBuilder
                    .AddRepository<
                        AppDbContext,
                        UserProfileProjectionDto,
                        UserProfileReadModelEntity,
                        UserProfileProjectionBulkEntityFactory>()
                    .AddCommandHandler<UserProfileProjectionDto>()
                    .AddConsumer<
                        AppDbContext,
                        UserProfileReadModel,
                        UserProfileProjectionDto,
                        Guid,
                        UserProfileProjectionValueFactory>());

        return services;
    }
}
