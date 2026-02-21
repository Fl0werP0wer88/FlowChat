using FlowChat.SocialGraphService.Worker.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.SocialGraphService.Worker;

public static class WorkerServiceRegistration
{
    public static IServiceCollection AddWorkerKafkaConsumer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<TemplateConsumerOptions>(
            configuration.GetSection(TemplateConsumerOptions.SectionName));

        services.AddHostedService<TemplateKafkaConsumerService>();

        return services;
    }
}
