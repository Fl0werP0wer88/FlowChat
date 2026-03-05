namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class EmailVerificationRequestIntegrationEvent : AuthIdentityTopicEventBase
    {
        public Guid UserId { get; set; }
        public required string UserEmail { get; set; }
        public required string ConfirmationLink { get; set; }
    }
}
