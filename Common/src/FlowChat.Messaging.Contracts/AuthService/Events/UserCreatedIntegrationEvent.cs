namespace FlowChat.Messaging.Contracts.AuthService.Events;

public sealed class UserCreatedIntegrationEvent : AuthIdentityTopicEventBase
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public required string UserName { get; init; }
    public required string DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
}
