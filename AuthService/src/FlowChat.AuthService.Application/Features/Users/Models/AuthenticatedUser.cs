namespace FlowChat.AuthService.Application.Features.Users.Models;

public class AuthenticatedUser
{
    public Guid Id { get; set; }
    public required string UserName { get; set; }
    public required string Email { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
