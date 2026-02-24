using Confluent.Kafka;
using FlowChat.AuthService.Persistence;
using FlowChat.AuthService.Persistence.Outbox;
using FlowChat.Messaging.Runtime.Kafka.GenericProducer;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<OutboxPublisherRuntimeOptions>(
    builder.Configuration.GetSection(OutboxPublisherRuntimeOptions.SectionName));

builder.Services.AddWorkerPersistenceServices(builder.Configuration);

builder.Services.AddOutboxTopic("flowchat.identity.user.created.v1");

builder.Services.AddSingleton<IProducer<string, string>>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<OutboxPublisherRuntimeOptions>>().Value;

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

builder.Services.AddHostedService<GenericKafkaOutboxPublisherBackgroundService<AppDbContext, OutboxMessage>>();

await builder.Build().RunAsync();
