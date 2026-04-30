namespace FlowChat.Core.Messaging.AuthService.Events;

public sealed record EmailVerificationRequestIntegrationEvent : IntegrationEvent
{
    public Guid VerificationRequestId { get; set; }
    public Guid UserId { get; set; }
    public required string UserEmail { get; set; }
    public required string ConfirmationLink { get; set; }
}
