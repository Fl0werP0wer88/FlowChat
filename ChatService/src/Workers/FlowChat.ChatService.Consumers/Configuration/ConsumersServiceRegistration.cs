using FlowChat.ChatService.Application;
using FlowChat.ChatService.Application.Features.UserProfile;
using FlowChat.ChatService.Consumers.Configuration.Settings;
using FlowChat.ChatService.Consumers.Kafka.Projections;
using FlowChat.ChatService.Infrastructure;
using FlowChat.ChatService.Persistence;
using FlowChat.ChatService.Persistence.Projections;
using FlowChat.Core.Messaging.UserProfileService.ReadModels;
using FlowChat.Shared.Consumers.Projections.Single;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.ChatService.Consumers;

public static class ConsumersServiceRegistration
{
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
            .AddFlowChatTieredRetryConsumerPipeline<AppDbContext>(
                consumerOptions.BootstrapServers,
                [consumerOptions])
            .WithConnectionToMessageBroker(options => options
                .AddKafka()
                .AddEntityFrameworkKafkaOffsetStore()
                .AddEntityFrameworkOutbox())
            .AddProjectionSingle<
                UserProfileReadModel,
                UserProfileProjectionDto,
                UserProfileProjectionValueFactory,
                UserProfileProjectionRepository>();

        return services;
    }
}
