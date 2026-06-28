using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
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
        services.AddScoped<IRealtimeConnectionCommandOrchestrator, RealtimeConnectionCommandOrchestrator>();

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.TypeEvaluator = type =>
                type == typeof(RouteMessageCommandHandler)
                || type == typeof(RoutePresenceChangeCommandHandler);
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }
}

