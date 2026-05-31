using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.EmailAdded;

public sealed class EmailAddedDomainEventHandler(
    IEmailVerificationProcessWriteRepository emailVerificationProcessWriteRepository,
    IEmailVerificationRequestIssuer emailVerificationRequestIssuer)
    : DomainEventHandlerBase<EmailAddedDomainEvent>
{
    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    protected override async Task ExecuteAsync(
        EmailAddedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var process = await _emailVerificationProcessWriteRepository
            .GetByEmailIdAsync(notification.EmailId.Value, cancellationToken);

        if (process is null)
        {
            process = EmailVerificationProcess.Create(
                Id<DomainUserProfile>.FromGuid(notification.UserProfileId.Value),
                Id<DomainEmail>.FromGuid(notification.EmailId.Value));

            await _emailVerificationProcessWriteRepository.AddAsync(process, cancellationToken);
        }

        await _emailVerificationRequestIssuer.IssueAsync(
            process,
            notification.UserProfileId.Value,
            notification.EmailId.Value,
            notification.Email.Value,
            cancellationToken);
    }
}
