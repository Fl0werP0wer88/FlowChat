namespace FlowChat.Messaging.Contracts.AuthService.Events;

public sealed class PhoneNumberConfirmedIntegrationEvent : IntegrationEvent
{
    public Guid UserId { get; init; }

    public required string PhoneNumber { get; init; }
}
