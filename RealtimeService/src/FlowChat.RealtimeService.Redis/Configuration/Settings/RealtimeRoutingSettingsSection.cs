using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Redis.Configuration.Settings;

public sealed class RealtimeRoutingSettingsSection : SettingsSectionBase
{
    public override string SectionName => "RealtimeRouting";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "flowchat:realtime";

    public TimeSpan ConnectionTtl { get; set; } = TimeSpan.FromMinutes(5);
}
