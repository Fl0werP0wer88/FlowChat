using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Testcontainers.Kafka;
using Testcontainers.PostgreSql;

namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessAATCollectionFixture : IAsyncLifetime
{
    private HarnessApiHost? _apiHost;
    private readonly KafkaContainer _kafkaContainer = new KafkaBuilder().Build();
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithDatabase("flowchat_harness_aat")
        .WithUsername("flowchat_harness")
        .WithPassword("flowchat_harness_pw")
        .Build();

    public const string CollectionName = "Harness AAT";
    public const string ApiKey = "FLOWCHAT_DEVELOPMENT_INTERNAL_API_KEY_CHANGE_ME";

    public string ConnectionString => _postgresContainer.GetConnectionString();
    public string BootstrapServers => _kafkaContainer.GetBootstrapAddress();
    public ProjectionKafkaAATSettings Projection { get; private set; } = null!;
    public RetryPipelineKafkaAATSettings RetryPipeline { get; private set; } = null!;

    public string ApiBaseUrl =>
        _apiHost?.BaseUrl
        ?? throw new InvalidOperationException("Harness API host has not been initialized.");

    public async Task InitializeAsync()
    {
        await Task.WhenAll(
            _postgresContainer.StartAsync(),
            _kafkaContainer.StartAsync());

        var topicPrefix = $"test.flowchat.harness.{Guid.NewGuid():N}";
        Projection = new ProjectionKafkaAATSettings(
            $"{topicPrefix}.projection.events",
            $"{topicPrefix}.projection.events.retry",
            $"{topicPrefix}.projection.events.dlq");
        RetryPipeline = new RetryPipelineKafkaAATSettings(
            $"{topicPrefix}.retry.events",
            [
                new($"{topicPrefix}.retry.events.retry", TimeSpan.FromMilliseconds(100)),
                new($"{topicPrefix}.retry.events.retry.250ms", TimeSpan.FromMilliseconds(250)),
                new($"{topicPrefix}.retry.events.retry.500ms", TimeSpan.FromMilliseconds(500)),
                new($"{topicPrefix}.retry.events.retry.1000ms", TimeSpan.FromMilliseconds(1000))
            ],
            $"{topicPrefix}.retry.events.dlq");

        await CreateTopicsAsync(
        [
            Projection.Topic,
            Projection.RetryTopic,
            Projection.DeadLetterTopic,
            RetryPipeline.Topic,
            .. RetryPipeline.RetryTiers.Select(x => x.Topic),
            RetryPipeline.DeadLetterTopic
        ]);

        _apiHost = new HarnessApiHost(ConnectionString, ApiKey);
        await _apiHost.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_apiHost is not null)
        {
            await _apiHost.DisposeAsync();
        }

        await _kafkaContainer.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private async Task CreateTopicsAsync(IEnumerable<string> topics)
    {
        using var adminClient = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = BootstrapServers
        }).Build();

        await adminClient.CreateTopicsAsync(topics.Select(topic => new TopicSpecification
        {
            Name = topic,
            NumPartitions = 1,
            ReplicationFactor = 1
        }));
    }
}
