namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public sealed class InternalApiSettingsSection
{
    public const string SectionName = "FlowChat:InternalApi";

    public string ApiKey { get; set; } = string.Empty;
}
