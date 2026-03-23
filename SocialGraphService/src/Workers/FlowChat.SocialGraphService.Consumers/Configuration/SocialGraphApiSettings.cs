namespace FlowChat.SocialGraphService.Consumers.Configuration;

public sealed class SocialGraphApiSettings
{
    public const string SectionName = "SocialGraphApi";

    public string BaseUrl { get; set; } = "https://localhost:7194";

    public string ApiKey { get; set; } = string.Empty;
}
