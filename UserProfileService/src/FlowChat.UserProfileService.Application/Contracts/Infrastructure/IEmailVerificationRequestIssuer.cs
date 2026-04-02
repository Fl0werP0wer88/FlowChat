using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Application.Contracts.Infrastructure;

public interface IEmailVerificationRequestIssuer
{
    Task<EmailVerificationRequest> IssueAsync(
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken);
}
