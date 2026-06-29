using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Infrastructure.Services;
using FlowChat.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.Infrastructure;

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
        => services.AddCommonInfrastructureServices(configuration);
}

internal static class CommonInfrastructureServiceRegistration
{
    public static IServiceCollection AddCommonInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSettingsSections(configuration, typeof(CommonInfrastructureServiceRegistration).Assembly);
        services.AddScoped<INotificationSender, NotificationSender>();
        return services;
    }
}
