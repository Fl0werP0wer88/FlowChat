namespace FlowChat.AuthService.Application.Features.Users.Models;

public class JwtTokenResult
{
    public required string AccessToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAtUtc { get; set; }
}
