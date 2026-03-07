namespace FlowChat.UserProfileService.Infrastructure.Configuration;

public sealed class ApiRuntimeSettings
{
    public string ApiUrl { get; set; } = "https://localhost:5000";

    public string BlazorUrl { get; set; } = "https://localhost:5010";

    public bool DropDatabaseOnStartup { get; set; }
}
