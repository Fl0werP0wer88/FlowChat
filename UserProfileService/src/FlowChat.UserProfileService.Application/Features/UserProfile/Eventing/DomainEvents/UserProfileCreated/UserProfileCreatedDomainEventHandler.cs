using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.UserProfile.Events;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Eventing.DomainEvents.UserProfileCreated;

public sealed class UserProfileCreatedDomainEventHandler(
    IEmailVerificationProcessWriteRepository emailVerificationProcessWriteRepository,
    IEmailVerificationRequestIssuer emailVerificationRequestIssuer)
    : DomainEventHandlerBase<UserProfileCreatedDomainEvent>
{
    private const string SystemActor = "system";

    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer = emailVerificationRequestIssuer;

    protected override async Task ExecuteAsync(
        UserProfileCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Issuing the verification request is a side effect of profile creation, done here
        // rather than in the command handler so the domain event is the single source of truth
        // for triggering the verification flow (including replays).
        var process = await _emailVerificationProcessWriteRepository
            .GetByEmailIdAsync(notification.MainEmailId.Value, cancellationToken);

        if (process is null)
        {
            process = EmailVerificationProcess.Create(
                Id<DomainUserProfile>.FromGuid(notification.UserProfileId.Value),
                Id<DomainEmail>.FromGuid(notification.MainEmailId.Value));

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
            notification.MainEmailId.Value,
            notification.MainEmail.Value,
            cancellationToken);
    }
}
