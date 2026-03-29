using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Infrastructure;

public interface IEmailVerificationRequestIssuer
{
    Task<EmailVerificationRequest> IssueAsync(
        Guid userProfileId,
        Guid emailId,
        string emailAddress,
        CancellationToken cancellationToken);
}
