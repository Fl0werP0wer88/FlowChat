namespace FlowChat.AuthService.API.Features.User.Public.RefreshToken;

public sealed class RefreshTokenResponse
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
