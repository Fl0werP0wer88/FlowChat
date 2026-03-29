using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IEmailVerificationRequestWriteRepository : IWriteRepository<EmailVerificationRequest>
{
    Task<IReadOnlyList<EmailVerificationRequest>> GetActiveByEmailIdAsync(
        Guid emailId,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationRequest?> GetByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default);
}
