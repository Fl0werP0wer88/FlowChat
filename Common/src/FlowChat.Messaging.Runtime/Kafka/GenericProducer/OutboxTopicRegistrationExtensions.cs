using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Messaging.Runtime.Kafka.GenericProducer;

public static class OutboxTopicRegistrationExtensions
{
    public static IServiceCollection AddOutboxTopic(
        this IServiceCollection services,
        string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic is required.", nameof(topic));
        }

        services.AddSingleton(new OutboxTopicRegistration(topic));
        return services;
    }
}
