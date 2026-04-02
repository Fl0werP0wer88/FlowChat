namespace FlowChat.AuthService.Application.Features.User.Commands.RefreshToken;

public class RefreshTokenCommandResponse
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
