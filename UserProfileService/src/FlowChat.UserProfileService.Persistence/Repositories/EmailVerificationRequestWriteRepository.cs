using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.Shared.Persistance;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
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
        var nowUtc = UtcDateTimeOffset.UtcNow;
        var query = _dbContext.EmailVerificationRequests
            .Where(x => x.EmailId == typedEmailId
                        && x.InvalidatedAtUtc == null
                        && x.ConsumedAtUtc == null);

        if (string.Equals(_dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.Ordinal))
        {
            return (await query.ToListAsync(cancellationToken))
                .Where(x => x.ExpiresAtUtc > nowUtc)
                .ToList();
        }

        return await query
            .Where(x => x.ExpiresAtUtc > nowUtc)
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

    public async Task<EmailVerificationConfirmationState?> GetConfirmationStateByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.EmailVerificationRequests
            .AsNoTracking()
            .Join(
                _dbContext.Emails.AsNoTracking(),
                request => request.EmailId,
                email => email.Id,
                (request, email) => new { Request = request, Email = email })
            .Where(x => x.Request.Nonce == nonce)
            .Select(x => new EmailVerificationConfirmationState(
                x.Request.UserProfileId.Value,
                x.Request.EmailId.Value,
                x.Request.InvalidatedAtUtc,
                x.Request.ConsumedAtUtc,
                x.Email.IsConfirmed))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
