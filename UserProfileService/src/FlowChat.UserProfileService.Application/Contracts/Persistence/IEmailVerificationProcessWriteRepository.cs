using FlowChat.Core.Contracts;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public sealed record EmailVerificationConfirmationState(
    Guid UserProfileId,
    Guid EmailId,
    UtcDateTimeOffset? InvalidatedAtUtc,
    UtcDateTimeOffset? ConsumedAtUtc,
    bool EmailIsConfirmed) : IDbReadResponse;

public interface IEmailVerificationProcessWriteRepository : IWriteRepository<EmailVerificationProcess>
{
    Task<EmailVerificationProcess?> GetByEmailIdAsync(
        Guid emailId,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationProcess?> GetByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationConfirmationState?> GetConfirmationStateByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default);
}
