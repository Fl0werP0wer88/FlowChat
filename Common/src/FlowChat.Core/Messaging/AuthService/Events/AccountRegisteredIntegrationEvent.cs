namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed class AccountRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required string FriendlyUserId { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
