using FlowChat.AuthService.Domain.Entities;
using FlowChat.Domain.Abstractions;

namespace FlowChat.AuthService.Domain.Events;

public sealed class EmailVerificationRequestedDomainEvent : BaseIdentityDomainEvent
{
    public Id<Identity> UserId { get; }
    public string UserEmail { get; }
    public string ConfirmationLink { get; }

    public EmailVerificationRequestedDomainEvent(
        Id<Identity> userId,
        string userEmail,
        string confirmationLink)
        : base(userId)
    {
        UserId = userId;
        UserEmail = !string.IsNullOrWhiteSpace(userEmail)
            ? userEmail
            : throw new ArgumentException("User email is required.", nameof(userEmail));
        ConfirmationLink = !string.IsNullOrWhiteSpace(confirmationLink)
            ? confirmationLink
            : throw new ArgumentException("Confirmation link is required.", nameof(confirmationLink));
    }
}
