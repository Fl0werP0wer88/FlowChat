using FlowChat.NotificationService.Application.Contracts.Infrastructure;
using FlowChat.NotificationService.Infrastructure.Configuration;
using FlowChat.NotificationService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FlowChat.NotificationService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.TryAddSingleton<IApiSettingsManager>(new ApiSettingsManager(configuration));
        services.AddScoped<INotificationSender, NotificationSender>();
        return services;
    }
}
