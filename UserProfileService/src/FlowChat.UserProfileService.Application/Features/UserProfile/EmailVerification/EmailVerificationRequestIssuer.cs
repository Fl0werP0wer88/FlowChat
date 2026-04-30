using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;

public sealed class EmailVerificationRequestIssuer(
    IEmailVerificationRequestWriteRepository emailVerificationRequestWriteRepository,
    IEmailVerificationTokenProtector emailVerificationTokenProtector,
    IEmailVerificationLinkBuilder emailVerificationLinkBuilder,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : IEmailVerificationRequestIssuer
{
    private readonly IEmailVerificationRequestWriteRepository _emailVerificationRequestWriteRepository = emailVerificationRequestWriteRepository;
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
        var activeRequests = await _emailVerificationRequestWriteRepository
            .GetActiveByEmailIdAsync(emailId, cancellationToken);

        foreach (var activeRequest in activeRequests)
        {
            activeRequest.Invalidate(nowUtc);
        }
        // ToDo: Think about theIdea: Verification RequestId should be generated otside of issuer (On Client) and passed in as parameter to provide idempotency. Instead cancelling alll active request and creating new one
        var verificationRequest = EmailVerificationRequest.Create(
            Id<EmailVerificationRequest>.New(),
            userProfileId,
            emailId,
            Guid.NewGuid().ToString("N"),
            nowUtc.AddHours(24));

        await _emailVerificationRequestWriteRepository.AddAsync(verificationRequest, cancellationToken);

        var token = _emailVerificationTokenProtector.Protect(
            new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce));
        var confirmationLink = _emailVerificationLinkBuilder.BuildEmailVerificationLink(token);

        await _integrationEventPublisher.Publish(
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
