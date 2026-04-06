namespace FlowChat.AuthService.API.Features.User.Public.RegisterUser;

public sealed class RegisterUserRequest
{
    public required string FriendlyUserId { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Organization { get; set; }
}
