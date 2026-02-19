namespace FlowChat.AuthService.Application.Responses;

public class LoginUserCommandResponse
{
    public bool IsSuccess { get; set; }
    public string? AccessToken { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
}
