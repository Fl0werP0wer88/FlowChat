namespace FlowChat.AuthService.Application.Features.User.Models;

public sealed class AuthenticatedAccount
{
    public Guid Id { get; set; }
    public required string FriendlyUserId { get; set; }
    public required string Email { get; set; }
    public IReadOnlyCollection<string> Roles { get; set; } = [];
}
