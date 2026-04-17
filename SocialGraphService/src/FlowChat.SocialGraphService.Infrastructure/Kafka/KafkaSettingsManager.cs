using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public ContactAddedProducerSettingsSection GetContactAddedProducerSettingsSection() =>
        _configuration.GetSection(ContactAddedProducerSettingsSection.SectionName).Get<ContactAddedProducerSettingsSection>()
        ?? new ContactAddedProducerSettingsSection();

    public ContactDeletedProducerSettingsSection GetContactDeletedProducerSettingsSection() =>
        _configuration.GetSection(ContactDeletedProducerSettingsSection.SectionName).Get<ContactDeletedProducerSettingsSection>()
        ?? new ContactDeletedProducerSettingsSection();
}
