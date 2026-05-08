using FlowChat.Shared.Application;
using FlowChat.Core.Contracts;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public sealed record EmailVerificationConfirmationState(
    Guid UserProfileId,
    Guid EmailId,
    UtcDateTimeOffset? InvalidatedAtUtc,
    UtcDateTimeOffset? ConsumedAtUtc,
    bool EmailIsConfirmed) : IDbReadResponse;

public interface IEmailVerificationRequestWriteRepository : IWriteRepository<EmailVerificationRequest>
{
    Task<IReadOnlyList<EmailVerificationRequest>> GetActiveByEmailIdAsync(
        Guid emailId,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationRequest?> GetByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationConfirmationState?> GetConfirmationStateByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default);
}
