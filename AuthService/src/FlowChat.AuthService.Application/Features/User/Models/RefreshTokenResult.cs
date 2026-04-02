namespace FlowChat.AuthService.Application.Features.User.Models;

public sealed class RefreshTokenResult
{
    public required string Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
