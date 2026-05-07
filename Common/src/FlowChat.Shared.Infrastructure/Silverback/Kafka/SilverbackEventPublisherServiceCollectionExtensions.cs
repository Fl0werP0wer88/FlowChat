using FlowChat.Shared.Application;
using Microsoft.Extensions.DependencyInjection;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public static class SilverbackEventPublisherServiceCollectionExtensions
{
    public static IServiceCollection AddFlowChatSilverbackEventPublisher(
        this IServiceCollection services,
        Action<KafkaProducerSettingsRegistryBuilder> configure)
    {
        var builder = new KafkaProducerSettingsRegistryBuilder();
        configure(builder);

        services.AddSingleton(sp => builder.Build(sp));
        services.AddScoped<FlowChatSilverbackEventPublisher>();
        services.AddScoped<IOutboxIntegrationEventPublisher>(sp =>
            sp.GetRequiredService<FlowChatSilverbackEventPublisher>());
        services.AddScoped<IDirectEventPublisher>(sp =>
            sp.GetRequiredService<FlowChatSilverbackEventPublisher>());

        return services;
    }
}
