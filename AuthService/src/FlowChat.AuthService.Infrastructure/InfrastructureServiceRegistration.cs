using FlowChat.Shared.Application;
using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Kafka;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.AuthService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<ISettingsProvider>(new SettingsProvider(configuration));

        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IOpenIddictTokenService, OpenIddictTokenService>();
        services.AddScoped<IKafkaProducerSettingsSection<AccountRegisteredIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<AccountRegisteredProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<AccountConfirmedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<AccountConfirmedProducerSettingsSection>());
        services.AddScoped<IKafkaProducerSettingsSection<PhoneNumberConfirmedIntegrationEvent>>(sp =>
            sp.GetRequiredService<ISettingsProvider>().GetSection<PhoneNumberConfirmedProducerSettingsSection>());
        services.AddScoped<IOutboxIntegrationEventPublisher, FlowChatSilverbackEventPublisher>();

        return services;
    }
}
