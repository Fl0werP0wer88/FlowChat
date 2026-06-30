using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.ProjectionBulk;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.SocialGraphService.Application;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Consumers.Configuration.Settings;
using FlowChat.SocialGraphService.Consumers.Kafka.Projections;
using FlowChat.SocialGraphService.Infrastructure;
using FlowChat.SocialGraphService.Persistence;
using FlowChat.SocialGraphService.Persistence.BulkUpsert.Projections;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

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
