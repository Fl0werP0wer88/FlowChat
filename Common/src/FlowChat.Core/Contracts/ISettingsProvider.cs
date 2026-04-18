using FlowChat.Core.Messaging;

namespace FlowChat.Core.Contracts;

public interface ISettingsProvider
{
    TSection GetSection<TSection>()
        where TSection : ISettingSection, new();

    TSection GetSectionForKafkaProducer<TSection, TEvent>()
        where TSection : IKafkaProducerSettingsSection<TEvent>, new()
        where TEvent : IntegrationEvent;
}
