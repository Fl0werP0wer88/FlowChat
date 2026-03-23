namespace FlowChat.NotificationService.Consumers.Configuration;

public sealed class NotificationApiSettings
{
    public const string SectionName = "NotificationApi";

    public string BaseUrl { get; set; } = "https://localhost:7206";

    public string ApiKey { get; set; } = string.Empty;
}
