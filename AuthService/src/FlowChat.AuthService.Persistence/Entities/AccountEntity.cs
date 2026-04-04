using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Persistence.Entities;

public sealed class AccountEntity
{
    public Guid Id { get; set; }
    public EmailAddress Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string FriendlyUserId { get; set; } = string.Empty;
    public string NormalizedFriendlyUserId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public int AccessFailedCount { get; set; }
    public bool IsEmailConfirmed { get; set; }
}
