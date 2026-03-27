using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        // The worker forwards Kafka events to the internal realtime API and does not
        // execute SignalR dispatch handlers locally.
        return services;
    }
}

