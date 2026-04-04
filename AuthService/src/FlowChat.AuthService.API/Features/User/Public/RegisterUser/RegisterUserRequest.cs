namespace FlowChat.AuthService.API.Features.User.Public.RegisterUser;

public sealed class RegisterUserRequest
{
    public required string FriendlyUserId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
}
