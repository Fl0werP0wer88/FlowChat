namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class PresenceServiceSettings
{
    public const string SectionName = "PresenceService";

    public string BaseUrl { get; set; } = string.Empty;

    public string InternalApiKey { get; set; } = string.Empty;
}
