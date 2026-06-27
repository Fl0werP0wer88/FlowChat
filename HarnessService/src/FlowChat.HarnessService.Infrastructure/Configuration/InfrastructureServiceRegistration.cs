using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }

    public static IServiceCollection AddApiInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }
}
