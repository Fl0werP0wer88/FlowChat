using Microsoft.Extensions.Configuration;

namespace FlowChat.SocialGraphService.Infrastructure.Kafka;

public sealed class KafkaSettingsManager(IConfiguration configuration) : IKafkaSettingsManager
{
    private readonly IConfiguration _configuration = configuration;

    public ContactAddedProducerOptions GetContactAddedProducerOptions() =>
        _configuration.GetSection(ContactAddedProducerOptions.SectionName).Get<ContactAddedProducerOptions>()
        ?? new ContactAddedProducerOptions();

    public ContactDeletedProducerOptions GetContactDeletedProducerOptions() =>
        _configuration.GetSection(ContactDeletedProducerOptions.SectionName).Get<ContactDeletedProducerOptions>()
        ?? new ContactDeletedProducerOptions();
}
