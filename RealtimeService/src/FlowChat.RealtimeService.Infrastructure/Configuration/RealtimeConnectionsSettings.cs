namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class RealtimeConnectionsSettings
{
    public const string SectionName = "RealtimeConnections";
    public const string RedisConnectionStringName = "Redis";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string InstanceId { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "flowchat:realtime";

    public TimeSpan ConnectionTtl { get; set; } = TimeSpan.FromMinutes(5);

    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(1);
}
