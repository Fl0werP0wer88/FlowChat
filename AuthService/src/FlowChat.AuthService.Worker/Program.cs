using Confluent.Kafka;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Worker.Outbox;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OutboxPublisherOptions>(
    builder.Configuration.GetSection(OutboxPublisherOptions.SectionName));

builder.Services.AddWorkerPersistenceServices(builder.Configuration);

builder.Services.AddSingleton<IProducer<string, string>>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<OutboxPublisherOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BootstrapServers))
    {
        throw new InvalidOperationException("Kafka bootstrap servers are not configured for the outbox publisher.");
    }

    var producerConfig = new ProducerConfig
    {
        BootstrapServers = options.BootstrapServers
    };

    return new ProducerBuilder<string, string>(producerConfig).Build();
});

builder.Services.AddHostedService<OutboxPublisherWorker>();

await builder.Build().RunAsync();
