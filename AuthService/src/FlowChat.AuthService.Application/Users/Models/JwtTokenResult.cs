namespace FlowChat.AuthService.Application.Users.Models;

public class JwtTokenResult
{
    public required string AccessToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
