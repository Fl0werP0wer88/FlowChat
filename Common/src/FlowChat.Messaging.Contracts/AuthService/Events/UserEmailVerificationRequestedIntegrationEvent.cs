namespace FlowChat.Messaging.Contracts.AuthService.Events
{
    public sealed class UserEmailVerificationRequestedIntegrationEvent
    {
        public Guid UserId { get; init; }
        public required string UserEmail { get; init; }
        public required string ConfirmationLink { get; init; }
    }
}