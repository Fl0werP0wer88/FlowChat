namespace FlowChat.AuthService.Application.Features.Users.Commands.RefreshToken;

public class RefreshTokenCommandResponse
{
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
