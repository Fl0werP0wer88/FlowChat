namespace FlowChat.AuthService.Consumers.Configuration;

public sealed class AuthApiSettings
{
    public const string SectionName = "AuthApi";

    public string BaseUrl { get; set; } = "https://localhost:7236";

    public string ApiKey { get; set; } = string.Empty;
}
