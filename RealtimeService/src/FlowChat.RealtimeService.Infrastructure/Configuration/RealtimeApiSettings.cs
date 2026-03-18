namespace FlowChat.RealtimeService.Infrastructure.Configuration;

public sealed class RealtimeApiSettings
{
    public const string SectionName = "RealtimeApi";

    public string BaseUrl { get; set; } = "http://localhost:5215";

    public string ApiKey { get; set; } = string.Empty;
}
