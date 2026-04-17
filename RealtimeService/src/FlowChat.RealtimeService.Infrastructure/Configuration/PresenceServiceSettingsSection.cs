namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class PresenceServiceSettingsSection
{
    public const string SectionName = "PresenceService";

    public string BaseUrl { get; set; } = string.Empty;

    public string InternalApiKey { get; set; } = string.Empty;
}
