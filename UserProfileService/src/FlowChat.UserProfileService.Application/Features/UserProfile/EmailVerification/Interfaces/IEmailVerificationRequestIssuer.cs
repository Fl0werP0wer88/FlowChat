using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;

public interface IEmailVerificationRequestIssuer
{
    Task<EmailVerificationRequest> IssueAsync(
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken);
}
