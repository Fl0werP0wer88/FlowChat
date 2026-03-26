using FlowChat.Application.Abstractions;
using FlowChat.AuthService.Application.Common.Eventing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.AuthService.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ApplicationServiceRegistration).Assembly;

        services.AddAutoMapper((Action<AutoMapper.IMapperConfigurationExpression>?)null, applicationAssembly);
        services.AddFlowChatValidatorsFromAssembly(applicationAssembly);
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(applicationAssembly);
            cfg.AddFlowChatBehaviors();
        });
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}

