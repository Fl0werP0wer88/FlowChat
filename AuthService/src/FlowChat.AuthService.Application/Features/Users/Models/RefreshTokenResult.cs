namespace FlowChat.AuthService.Application.Features.Users.Models;

public sealed class RefreshTokenResult
{
    public required string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
