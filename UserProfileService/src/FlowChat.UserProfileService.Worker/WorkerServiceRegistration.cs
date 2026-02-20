using FlowChat.UserProfileService.Worker.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.UserProfileService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<UserCreatedConsumerOptions>(
            configuration.GetSection(UserCreatedConsumerOptions.SectionName));

        services.AddHostedService<UserCreatedKafkaConsumerService>();

        return services;
    }
}
