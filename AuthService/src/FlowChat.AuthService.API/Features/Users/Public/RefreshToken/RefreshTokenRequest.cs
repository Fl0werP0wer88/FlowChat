namespace FlowChat.AuthService.API.Features.Users.Public.RefreshToken;

public sealed class RefreshTokenRequest
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}
