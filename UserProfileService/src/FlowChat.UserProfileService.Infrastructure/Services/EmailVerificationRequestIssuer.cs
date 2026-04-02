using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Infrastructure.Services;

public sealed class EmailVerificationRequestIssuer(
    IEmailVerificationRequestWriteRepository emailVerificationRequestWriteRepository,
    IEmailVerificationTokenProtector emailVerificationTokenProtector,
    IEmailVerificationLinkBuilder emailVerificationLinkBuilder,
    IIntegrationEventPublisher integrationEventPublisher)
    : IEmailVerificationRequestIssuer
{
    private readonly IEmailVerificationRequestWriteRepository _emailVerificationRequestWriteRepository = emailVerificationRequestWriteRepository;
    private readonly IEmailVerificationTokenProtector _emailVerificationTokenProtector = emailVerificationTokenProtector;
    private readonly IEmailVerificationLinkBuilder _emailVerificationLinkBuilder = emailVerificationLinkBuilder;
    private readonly IIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher;

    public async Task<EmailVerificationRequest> IssueAsync(
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);

        var nowUtc = DateTime.UtcNow;
        var activeRequests = await _emailVerificationRequestWriteRepository
            .GetActiveByEmailIdAsync(emailId, cancellationToken);

        foreach (var activeRequest in activeRequests)
        {
            activeRequest.Invalidate(nowUtc);
        }

        var verificationRequest = EmailVerificationRequest.Create(
            userProfileId,
            emailId,
            Guid.NewGuid().ToString("N"),
            nowUtc.AddHours(24));

        await _emailVerificationRequestWriteRepository.AddAsync(verificationRequest, cancellationToken);

        var token = _emailVerificationTokenProtector.Protect(
            new EmailVerificationTokenPayload(userProfileId, emailId, verificationRequest.Nonce));
        var confirmationLink = _emailVerificationLinkBuilder.BuildEmailVerificationLink(token);

        await _integrationEventPublisher.PublishToOutboxAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                Key = verificationRequest.Id.Value.ToString(),
                UserId = userProfileId,
                UserEmail = emailAddress,
                ConfirmationLink = confirmationLink
            },
            cancellationToken);

        return verificationRequest;
    }
}
