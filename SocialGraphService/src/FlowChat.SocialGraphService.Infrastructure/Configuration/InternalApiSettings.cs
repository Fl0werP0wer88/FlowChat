namespace FlowChat.SocialGraphService.Infrastructure.Configuration;

public sealed class InternalApiSettings
{
    public const string SectionName = "FlowChat:InternalApi";

    public string ApiKey { get; set; } = string.Empty;
}
