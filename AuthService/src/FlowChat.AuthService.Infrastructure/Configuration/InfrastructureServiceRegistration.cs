using FlowChat.AuthService.Application.Contracts.Infrastructure;
using FlowChat.AuthService.Infrastructure.Configuration.Settings;
using FlowChat.AuthService.Infrastructure.Services;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Infrastructure.Configuration;
using FlowChat.Shared.Infrastructure.Silverback.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Infrastructure;

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

internal static class CommonInfrastructureServiceRegistration
{
    public static IServiceCollection AddCommonInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(CommonInfrastructureServiceRegistration).Assembly);

        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IOpenIddictTokenService, OpenIddictTokenService>();
        services.AddFlowChatSilverbackEventPublisher(producer => producer
            .AddProducerSettings<AccountRegisteredIntegrationEvent, AccountRegisteredProducerSettingsSection>()
            .AddProducerSettings<AccountConfirmedIntegrationEvent, AccountConfirmedProducerSettingsSection>()
            .AddProducerSettings<PhoneNumberConfirmedIntegrationEvent, PhoneNumberConfirmedProducerSettingsSection>());

        return services;
    }
}
