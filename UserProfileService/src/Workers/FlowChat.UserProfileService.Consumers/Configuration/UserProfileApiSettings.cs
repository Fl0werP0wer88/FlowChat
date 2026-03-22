namespace FlowChat.UserProfileService.Consumers.Configuration;

public sealed class UserProfileApiSettings
{
    public const string SectionName = "UserProfileApi";

    public string BaseUrl { get; set; } = "https://localhost:7148";

    public string ApiKey { get; set; } = string.Empty;
}
