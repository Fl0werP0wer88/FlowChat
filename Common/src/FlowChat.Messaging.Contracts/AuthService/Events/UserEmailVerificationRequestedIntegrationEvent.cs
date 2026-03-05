namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class EmailVerificationRequestIntegrationEvent : IntegrationEventBase
    {
        public Guid UserId { get; set; }
        public required string UserEmail { get; set; }
        public required string ConfirmationLink { get; set; }
    }
}
