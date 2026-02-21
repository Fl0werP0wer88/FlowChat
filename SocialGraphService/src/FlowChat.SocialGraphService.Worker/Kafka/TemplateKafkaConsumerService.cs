using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FlowChat.SocialGraphService.Worker.Kafka;

public sealed class TemplateKafkaConsumerService : BackgroundService
{
    private readonly ILogger<TemplateKafkaConsumerService> _logger;
    private readonly TemplateConsumerOptions _options;

    public TemplateKafkaConsumerService(
        ILogger<TemplateKafkaConsumerService> logger,
        IOptions<TemplateConsumerOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Template consumer initialized. BootstrapServers: {BootstrapServers}, GroupId: {GroupId}, Topic: {Topic}",
            _options.BootstrapServers,
            _options.GroupId,
            _options.Topic);

        // Template intentionally does not consume messages yet.
        return Task.CompletedTask;
    }
}
