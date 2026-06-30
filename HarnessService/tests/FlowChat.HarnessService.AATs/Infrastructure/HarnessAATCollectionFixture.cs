namespace FlowChat.HarnessService.AATs.Infrastructure;

public sealed class HarnessAATCollectionFixture : IAsyncLifetime
{
    private HarnessApiHost? _apiHost;

    public const string CollectionName = "Harness AAT";
    public const string ConnectionString = "Host=localhost;Port=5432;Database=flowchat_harness_db;Username=flowchat_app;Password=flowchat_app_pw;";
    public const string BootstrapServers = "localhost:9092";
    public const string Topic = "test.flowchat.harness.projection.events";
    public const string RetryTopic = "test.flowchat.harness.projection.events.retry";
    public const string DeadLetterTopic = "test.flowchat.harness.projection.events.dlq";
    public const string ApiKey = "FLOWCHAT_DEVELOPMENT_INTERNAL_API_KEY_CHANGE_ME";

    public string ApiBaseUrl =>
        _apiHost?.BaseUrl
        ?? throw new InvalidOperationException("Harness API host has not been initialized.");

    public async Task InitializeAsync()
    {
        _apiHost = new HarnessApiHost(ConnectionString, ApiKey);
        await _apiHost.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_apiHost is not null)
        {
            await _apiHost.DisposeAsync();
        }
    }
}
