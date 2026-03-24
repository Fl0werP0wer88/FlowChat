using FlowChat.Application.Abstractions;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Application;

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
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var consumerAssembly = typeof(CreateInitialUserProfileCommandHandler).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(consumerAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, consumerAssembly);
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
