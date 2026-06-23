namespace FlowChat.AuthService.Domain.Entities.Account;

public sealed record AccountSnapshot(
    Guid Id,
    string FriendlyUserId,
    string Email,
    string SecurityStamp,
    int AccessFailedCount,
    bool IsEmailConfirmed);
