using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Infrastructure;

public interface IEmailVerificationRequestIssuer
{
    Task<EmailVerificationRequest> IssueAsync(
        UserProfile userProfile,
        Email email,
        CancellationToken cancellationToken);
}
