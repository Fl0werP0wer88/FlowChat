namespace FlowChat.AuthService.Application.Models;

public sealed class UserCreatedEvent
{
    public Guid UserId { get; init; }
    public required string UserName { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Bio { get; init; }
}
