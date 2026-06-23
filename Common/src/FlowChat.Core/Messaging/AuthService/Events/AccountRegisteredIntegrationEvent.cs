namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed record AccountRegisteredIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string? Email { get; init; }
    public required string FriendlyUserId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Organization { get; init; }
}
