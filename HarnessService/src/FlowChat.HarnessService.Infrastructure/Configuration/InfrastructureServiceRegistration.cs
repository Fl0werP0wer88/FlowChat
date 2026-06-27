using FlowChat.HarnessService.Application.Contracts.Infrastructure;
using FlowChat.HarnessService.Infrastructure.Kafka;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.HarnessService.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddConsumerInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IConsumerOffsetStore, ConsumerOffsetStore>();

        return services;
    }

    public static IServiceCollection AddApiInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IConsumerOffsetStore, NullConsumerOffsetStore>();

        return services;
    }
}
