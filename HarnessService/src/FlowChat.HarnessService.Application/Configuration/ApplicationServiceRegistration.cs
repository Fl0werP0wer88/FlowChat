using FlowChat.Shared.Application;
using FlowChat.Shared.Application.Common.Eventing;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Application;

public static class ApiApplicationServiceRegistration
{
    public static IServiceCollection AddApiApplicationServices(this IServiceCollection services)
        => services.AddCommonApplicationServices();
}

public static class ConsumerApplicationServiceRegistration
{
    public static IServiceCollection AddConsumerApplicationServices(this IServiceCollection services)
        => services.AddCommonApplicationServices();
}

internal static class CommonApplicationServiceRegistration
{
    public static IServiceCollection AddCommonApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(CommonApplicationServiceRegistration).Assembly;

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
