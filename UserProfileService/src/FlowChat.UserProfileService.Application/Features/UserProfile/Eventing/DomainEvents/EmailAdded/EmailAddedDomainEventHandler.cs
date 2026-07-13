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
    : IDomainEventHandler<EmailAddedDomainEvent>
{
    private const string SystemActor = "system";

    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    public async Task Handle(
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

            // Created outside AggregateRootCommandHandlerBaseV2, so audit info must be set explicitly here
            process.SetCreated(SystemActor);
            process.SetUpdated(SystemActor);

            await _emailVerificationProcessWriteRepository.AddAsync(process, cancellationToken);
        }
        else
        {
            process.SetUpdated(SystemActor);
        }

        await _emailVerificationRequestIssuer.IssueAsync(
            process,
            notification.UserProfileId.Value,
            notification.EmailId.Value,
            notification.Email.Value,
            cancellationToken);
    }
}
