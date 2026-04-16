namespace FlowChat.RealtimeService.Consumers.Configuration;

public sealed class RealtimeApiSettings
{
    public const string SectionName = "RealtimeApi";

    public string ApiKey { get; set; } = string.Empty;

    public Dictionary<string, string> Instances { get; set; } = new(StringComparer.Ordinal);
}
