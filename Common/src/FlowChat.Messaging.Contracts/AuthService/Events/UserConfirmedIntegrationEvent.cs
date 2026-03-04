namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class UserConfirmedIntegrationEvent
    {
        public Guid UserId { get; init; }
    }
}
