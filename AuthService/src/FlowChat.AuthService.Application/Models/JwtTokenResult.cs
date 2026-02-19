namespace FlowChat.AuthService.Application.Models;

public class JwtTokenResult
{
    public required string AccessToken { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
