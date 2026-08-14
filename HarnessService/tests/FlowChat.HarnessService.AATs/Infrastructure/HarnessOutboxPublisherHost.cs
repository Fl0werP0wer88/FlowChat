using FlowChat.HarnessService.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessOutboxPublisherHost(
    string connectionString,
    string bootstrapServers,
    IEnumerable<string> destinationTopics) : IAsyncDisposable
{
    private readonly IReadOnlyCollection<string> _destinationTopics = destinationTopics.ToArray();
    private IHost? _host;

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Replace(ServiceDescriptor.Singleton<IHostLifetime, NoOpHostLifetime>());
        builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        builder.Services.AddDbContextFactory<AppDbContext>(
            options => options.UseNpgsql(connectionString),
            ServiceLifetime.Scoped);
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        builder.Services.AddSilverback()
            .WithConnectionToMessageBroker(options =>
            {
                options.AddKafka();
                options.AddEntityFrameworkOutbox();
                options.AddOutboxWorker(worker => worker
                    .ProcessOutbox(outbox => outbox.UseEntityFramework<AppDbContext>())
                    .WithBatchSize(100)
                    .WithInterval(TimeSpan.FromMilliseconds(50))
                    .WithExponentialRetryDelay(
                        TimeSpan.FromMilliseconds(50),
                        2,
                        TimeSpan.FromSeconds(1))
                    .WithoutDistributedLock());
            })
            .AddKafkaClients(clients =>
            {
                clients.WithBootstrapServers(bootstrapServers);
                foreach (var topic in _destinationTopics)
                {
                    clients.AddProducer(producer => producer
                        .Produce(topic, endpoint => endpoint
                            .ProduceTo(topic)
                            .SerializeAsJson(serializer => serializer.SetTypeHeader())));
                }
            });

        _host = builder.Build();
        await _host.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is null)
            return;

        await _host.StopAsync();
        _host.Dispose();
        _host = null;
    }
}
