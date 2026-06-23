using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Infrastructure.Configuration.Settings;

public sealed class PresenceStatusSettingsSection : SettingsSectionBase
{
    public override string SectionName => "PresenceStatus";
    public const string RedisConnectionStringName = "Redis";

    public string RedisConnectionString { get; set; } = string.Empty;

    public string KeyPrefix { get; set; } = "flowchat:presence";

    public TimeSpan PresenceTtl { get; set; } = TimeSpan.FromMinutes(2);
}
