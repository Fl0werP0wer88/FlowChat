using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;

public sealed class EmailVerificationRequestIssuer(
    IEmailVerificationProcessWriteRepository emailVerificationProcessWriteRepository,
    IEmailVerificationTokenProtector emailVerificationTokenProtector,
    IEmailVerificationLinkBuilder emailVerificationLinkBuilder,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : IEmailVerificationRequestIssuer
{
    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationTokenProtector _emailVerificationTokenProtector = emailVerificationTokenProtector;
    private readonly IEmailVerificationLinkBuilder _emailVerificationLinkBuilder = emailVerificationLinkBuilder;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher;

    public async Task<EmailVerificationRequest> IssueAsync(
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var nowUtc = UtcDateTimeOffset.UtcNow;
        var process = await _emailVerificationProcessWriteRepository
            .GetByEmailIdAsync(emailId, cancellationToken);

        if (process is null)
        {
            process = EmailVerificationProcess.Create(
                Id<DomainUserProfile>.FromGuid(userProfileId),
                Id<DomainEmail>.FromGuid(emailId));

            await _emailVerificationProcessWriteRepository.AddAsync(process, cancellationToken);
        }

        // ToDo: Think about theIdea: Verification RequestId should be generated otside of issuer (On Client) and passed in as parameter to provide idempotency. Instead cancelling alll active request and creating new one
        var verificationRequest = process.IssueRequest(
            Id<EmailVerificationRequest>.New(),
            Guid.NewGuid().ToString("N"),
            nowUtc.AddHours(24),
            nowUtc);

        var token = _emailVerificationTokenProtector.Protect(
            new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce));
        var confirmationLink = _emailVerificationLinkBuilder.BuildEmailVerificationLink(token);

        await _integrationEventPublisher.PublishAsync(
            new IntegrationEventEnvelope<EmailVerificationRequestIntegrationEvent>(
                new EmailVerificationRequestIntegrationEvent
                {
                    VerificationRequestId = verificationRequest.Id.Value,
                    UserId = userProfileId,
                    UserEmail = emailAddress,
                    ConfirmationLink = confirmationLink
                },
                verificationRequest.Id.Value.ToString()),
            cancellationToken);

        return verificationRequest;
    }
}
