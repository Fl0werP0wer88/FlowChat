using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddScoped<ILocalEventDispatcher, LocalEventDispatcher>();

        return services;
    }
}
