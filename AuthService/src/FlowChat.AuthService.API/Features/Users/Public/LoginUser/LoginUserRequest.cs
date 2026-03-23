namespace FlowChat.AuthService.API.Features.Users.Public.LoginUser;

public sealed class LoginUserRequest
{
    public required string Login { get; set; }
    public required string Password { get; set; }
}
