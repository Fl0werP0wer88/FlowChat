using FlowChat.Application.Abstractions;
using FlowChat.UserProfileService.Application.Common.Eventing;
using FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var consumerAssembly = typeof(CreateInitialUserProfileCommandHandler).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(consumerAssembly));
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
