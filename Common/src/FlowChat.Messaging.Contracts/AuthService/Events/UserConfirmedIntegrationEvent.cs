namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class UserConfirmedIntegrationEvent : AuthIdentityTopicEventBase
    {
        public Guid UserId { get; init; }
    }
}
