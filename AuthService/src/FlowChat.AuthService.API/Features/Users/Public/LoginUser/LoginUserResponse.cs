namespace FlowChat.AuthService.API.Features.Users.Public.LoginUser;

public sealed class LoginUserResponse
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
