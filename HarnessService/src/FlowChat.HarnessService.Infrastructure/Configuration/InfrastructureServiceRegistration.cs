using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Infrastructure;

public static class ConsumerInfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }
}

public static class ApiInfrastructureServiceRegistration
{
    public static IServiceCollection AddApiInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }
}
