namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class UserConfirmedIntegrationEvent : IntegrationEvent
    {
        public Guid UserId { get; init; }
    }
}
