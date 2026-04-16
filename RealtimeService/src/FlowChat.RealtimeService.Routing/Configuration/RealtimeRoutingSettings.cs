namespace FlowChat.RealtimeService.Routing.Configuration;

public sealed class RealtimeRoutingSettings
{
    public const string SectionName = "RealtimeRouting";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "flowchat:realtime";

    public TimeSpan ConnectionTtl { get; set; } = TimeSpan.FromMinutes(5);
}
