using FlowChat.UserProfileService.Application.UserProfiles.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(applicationAssembly));

        return services;
    }

    public static IServiceCollection AddWorkerApplicationServices(this IServiceCollection services)
    {
        var consumerAssembly = typeof(CreateInitialUserProfileCommandHandler).Assembly;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(consumerAssembly));

        return services;
    }
}
