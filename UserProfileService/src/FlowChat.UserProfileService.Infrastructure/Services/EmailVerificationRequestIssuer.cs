using FlowChat.Core.Messaging.AuthService.Events;
using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities;

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
        UserProfile userProfile,
        Email email,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(userProfile);
        ArgumentNullException.ThrowIfNull(email);

        var nowUtc = DateTime.UtcNow;
        var activeRequests = await _emailVerificationRequestWriteRepository
            .GetActiveByEmailIdAsync(email.Id.Value, cancellationToken);

        foreach (var activeRequest in activeRequests)
        {
            activeRequest.Invalidate(nowUtc);
        }

        var verificationRequest = EmailVerificationRequest.Create(
            userProfile.Id,
            email.Id,
            Guid.NewGuid().ToString("N"),
            nowUtc.AddHours(24));

        await _emailVerificationRequestWriteRepository.AddAsync(verificationRequest, cancellationToken);

        var token = _emailVerificationTokenProtector.Protect(
            new EmailVerificationTokenPayload(userProfile.Id.Value, email.Id.Value, verificationRequest.Nonce));
        var confirmationLink = _emailVerificationLinkBuilder.BuildEmailVerificationLink(token);

        await _integrationEventPublisher.PublishToOutboxAsync(
            new EmailVerificationRequestIntegrationEvent
            {
                Key = verificationRequest.Id.Value.ToString(),
                UserId = userProfile.Id.Value,
                UserEmail = email.Address.Value,
                ConfirmationLink = confirmationLink
            },
            cancellationToken);

        return verificationRequest;
    }
}
