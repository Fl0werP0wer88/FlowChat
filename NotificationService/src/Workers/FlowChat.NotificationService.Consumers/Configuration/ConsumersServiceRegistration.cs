using FlowChat.NotificationService.Application;
using FlowChat.NotificationService.Consumers.Configuration.Settings;
using FlowChat.NotificationService.Consumers.Kafka;
using FlowChat.NotificationService.Infrastructure;
using FlowChat.NotificationService.Persistence;
using FlowChat.Shared.Infrastructure.Silverback.Behaviors;
using FlowChat.Shared.Infrastructure.Silverback.Kafka.Retry.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.NotificationService.Consumers;

public static class ConsumersServiceRegistration
{
    public static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var consumerOptions = configuration
            .GetSection(new UserEmailVerificationRequestedConsumerSettingsSection().SectionName)
            .Get<UserEmailVerificationRequestedConsumerSettingsSection>()
            ?? new UserEmailVerificationRequestedConsumerSettingsSection();

        services.AddConsumerApplicationServices();
        services.AddConsumerInfrastructureServices(configuration);
        services.AddConsumerPersistenceServices(configuration);

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
            .AddScopedSubscriber<UserEmailVerificationRequestedSubscriber>();

        return services;
    }
}


