using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification.Interfaces;

public interface IEmailVerificationRequestIssuer
{
    Task<EmailVerificationRequest> IssueAsync(
        EmailVerificationProcess process,
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken);
}
