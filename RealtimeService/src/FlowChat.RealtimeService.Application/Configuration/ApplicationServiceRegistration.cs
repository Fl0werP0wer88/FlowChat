using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationMembershipDeltaV2;
using FlowChat.RealtimeService.Application.Features.Conversation.Commands.RouteConversationProjectionV2;
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
            cfg.TypeEvaluator = type => !RealtimeApplicationHandlerSets.ConsumerRouteHandlerTypes.Contains(type);
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
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
            cfg.TypeEvaluator = RealtimeApplicationHandlerSets.ConsumerRouteHandlerTypes.Contains;
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });

        return services;
    }
}

file static class RealtimeApplicationHandlerSets
{
    public static readonly Type[] ConsumerRouteHandlerTypes =
    [
        typeof(RouteMessageCommandHandler),
        typeof(RoutePresenceChangeCommandHandler),
        typeof(RouteConversationProjectionV2CommandHandler),
        typeof(RouteConversationMembershipDeltaV2CommandHandler)
    ];
}
