namespace FlowChat.AuthService.Application.Features.Users.Commands.LoginUser;

public class LoginUserCommandResponse
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiresAtUtc { get; set; }
}
