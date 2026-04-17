using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Messaging.AuthService.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;

namespace FlowChat.AuthService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.TryAddSingleton<IKafkaSettingsManager>(new KafkaSettingsManager(configuration));

        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IOpenIddictTokenService, OpenIddictTokenService>();
        services.AddScoped<IKafkaProducerSettingsSection<AccountRegisteredIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetAccountRegisteredProducerSettingsSection());
        services.AddScoped<IKafkaProducerSettingsSection<AccountConfirmedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetAccountConfirmedProducerSettingsSection());
        services.AddScoped<IKafkaProducerSettingsSection<PhoneNumberConfirmedIntegrationEvent>>(sp =>
            sp.GetRequiredService<IKafkaSettingsManager>().GetPhoneNumberConfirmedProducerSettingsSection());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}

