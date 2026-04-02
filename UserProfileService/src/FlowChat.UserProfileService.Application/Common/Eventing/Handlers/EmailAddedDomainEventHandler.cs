using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;

namespace FlowChat.UserProfileService.Application.Common.Eventing.Handlers;

public sealed class EmailAddedDomainEventHandler(
    IEmailVerificationRequestIssuer emailVerificationRequestIssuer)
    : DomainEventHandlerBase<EmailAddedDomainEvent>
{
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    protected override async Task ExecuteAsync(
        EmailAddedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        await _emailVerificationRequestIssuer.IssueAsync(
            notification.UserProfileId.Value,
            notification.EmailId.Value,
            notification.Email.Value,
            cancellationToken);
    }
}
