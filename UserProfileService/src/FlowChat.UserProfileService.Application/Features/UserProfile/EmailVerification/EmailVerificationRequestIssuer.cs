using FlowChat.Core.Messaging;
using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;

public sealed class EmailVerificationRequestIssuer(
    IEmailVerificationTokenProtector emailVerificationTokenProtector,
    IEmailVerificationLinkBuilder emailVerificationLinkBuilder,
    IOutboxIntegrationEventPublisher integrationEventPublisher)
    : IEmailVerificationRequestIssuer
{
    private readonly IEmailVerificationTokenProtector _emailVerificationTokenProtector = emailVerificationTokenProtector;
    private readonly IEmailVerificationLinkBuilder _emailVerificationLinkBuilder = emailVerificationLinkBuilder;
    private readonly IOutboxIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher;

    public async Task<EmailVerificationRequest> IssueAsync(
        EmailVerificationProcess process,
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var nowUtc = UtcDateTimeOffset.UtcNow;

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
