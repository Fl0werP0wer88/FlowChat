using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class EmailVerificationRequestWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<EmailVerificationRequest>(dbContext), IEmailVerificationRequestWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<EmailVerificationRequest>> GetActiveByEmailIdAsync(
        Guid emailId,
        CancellationToken cancellationToken = default)
    {
        var typedEmailId = Id<Email>.FromGuid(emailId);
        var nowUtc = DateTime.UtcNow;

        return await _dbContext.EmailVerificationRequests
            .Where(x => x.EmailId == typedEmailId
                        && x.InvalidatedAtUtc == null
                        && x.ConsumedAtUtc == null
                        && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailVerificationRequest?> GetByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailVerificationRequests
            .FirstOrDefaultAsync(
                x => x.Nonce == nonce,
                cancellationToken);
    }
}
