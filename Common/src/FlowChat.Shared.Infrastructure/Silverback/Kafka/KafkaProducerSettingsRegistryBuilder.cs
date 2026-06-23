using FlowChat.Core.Contracts;
using FlowChat.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FlowChat.Shared.Infrastructure.Silverback.Kafka;

public sealed class KafkaProducerSettingsRegistryBuilder
{
    private readonly Dictionary<Type, Func<IServiceProvider, IKafkaProducerSettingsSection>> _factories = new();

    public KafkaProducerSettingsRegistryBuilder AddProducerSettings<TEvent, TSettings>()
        where TEvent : IntegrationEvent
        where TSettings : class, IKafkaProducerSettingsSection<TEvent>, new()
    {
        _factories[typeof(TEvent)] = sp =>
            sp.GetRequiredService<IOptions<TSettings>>().Value;
        return this;
    }

    internal KafkaProducerSettingsRegistry Build(IServiceProvider sp)
    {
        var map = _factories.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value(sp));
        return new KafkaProducerSettingsRegistry(map);
    }
}
