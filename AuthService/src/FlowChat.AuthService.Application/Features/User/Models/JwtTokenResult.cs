namespace FlowChat.AuthService.Application.Features.User.Models;

public class JwtTokenResult
{
    public required string AccessToken { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public required string RefreshToken { get; set; }
    public DateTimeOffset RefreshTokenExpiresAtUtc { get; set; }
}
