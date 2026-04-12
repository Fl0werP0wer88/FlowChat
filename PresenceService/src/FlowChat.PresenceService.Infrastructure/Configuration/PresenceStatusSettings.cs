namespace FlowChat.PresenceService.Infrastructure.Configuration;

public sealed class PresenceStatusSettings
{
    public const string SectionName = "PresenceStatus";
    public const string RedisConnectionStringName = "Redis";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "flowchat:presence";
}
