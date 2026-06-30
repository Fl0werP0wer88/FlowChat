using FlowChat.RealtimeService.Application.Features.RealtimeConnection.Commands;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteGroupConversationChanged;
using FlowChat.RealtimeService.Application.Features.Message.Commands.RouteMessage;
using FlowChat.RealtimeService.Application.Features.Presence.Commands.RoutePresenceChange;
using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.RealtimeService.Application;

public static class ApiApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApiApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.TypeEvaluator = type =>
                type != typeof(RouteMessageCommandHandler)
                && type != typeof(RoutePresenceChangeCommandHandler)
                && type != typeof(RouteGroupConversationChangedCommandHandler);
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddScoped<IRealtimeConnectionCommandOrchestrator, RealtimeConnectionCommandOrchestrator>();

        return services;
    }
}

public static class ConsumerApplicationServiceRegistration
{
    public static IServiceCollection AddConsumerApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ConsumerApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.TypeEvaluator = type =>
                type == typeof(RouteMessageCommandHandler)
                || type == typeof(RoutePresenceChangeCommandHandler)
                || type == typeof(RouteGroupConversationChangedCommandHandler);
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }
}

