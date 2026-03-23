namespace FlowChat.AuthService.API.Features.Users.Public.RegisterUser;

public sealed class RegisterUserRequest
{
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public required string Password { get; set; }
}
