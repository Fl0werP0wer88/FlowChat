namespace FlowChat.AuthService.Infrastructure.Configuration;

public sealed class ApiRuntimeSettings
{
    public const string FlowChatSectionName = "FlowChat";

    public string ApiUrl { get; set; } = "https://localhost:5000";

    public string BlazorUrl { get; set; } = "https://localhost:5010";
}
