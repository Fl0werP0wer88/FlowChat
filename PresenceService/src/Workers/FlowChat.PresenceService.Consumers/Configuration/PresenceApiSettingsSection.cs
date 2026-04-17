namespace FlowChat.PresenceService.Consumers.Configuration;

public sealed class PresenceApiSettingsSection
{
    public const string SectionName = "PresenceApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
