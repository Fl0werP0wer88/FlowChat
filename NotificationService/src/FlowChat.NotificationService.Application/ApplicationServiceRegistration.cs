using FlowChat.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.NotificationService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }
}
