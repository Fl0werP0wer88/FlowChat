using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class EmailVerificationProcessWriteRepository(AppDbContext dbContext)
    : WriteRepositoryBase<EmailVerificationProcess>(dbContext), IEmailVerificationProcessWriteRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<EmailVerificationProcess?> GetByEmailIdAsync(
        Guid emailId,
        CancellationToken cancellationToken = default)
    {
        var typedProcessId = Id<EmailVerificationProcess>.FromGuid(emailId);

        return await _dbContext.EmailVerificationProcesses
            .Include(x => x.Requests)
            .FirstOrDefaultAsync(x => x.Id == typedProcessId, cancellationToken);
    }

    public async Task<EmailVerificationProcess?> GetByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        var normalizedNonce = nonce.Trim();

        return await _dbContext.EmailVerificationProcesses
            .Include(x => x.Requests)
            .FirstOrDefaultAsync(
                x => x.Requests.Any(request => request.Nonce == normalizedNonce),
                cancellationToken);
    }

    public async Task<EmailVerificationConfirmationState?> GetConfirmationStateByNonceAsync(
        string nonce,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);
        var normalizedNonce = nonce.Trim();

        return await _dbContext.Set<EmailVerificationRequest>()
            .AsNoTracking()
            .Where(request => request.Nonce == normalizedNonce)
            .Join(
                _dbContext.EmailVerificationProcesses.AsNoTracking(),
                request => EF.Property<Id<EmailVerificationProcess>>(request, "EmailVerificationProcessId"),
                process => process.Id,
                (request, process) => new { Request = request, Process = process })
            .Join(
                _dbContext.Emails.AsNoTracking(),
                x => x.Process.EmailId,
                email => email.Id,
                (x, email) => new { x.Request, x.Process, Email = email })
            .Select(x => new EmailVerificationConfirmationState(
                x.Process.UserProfileId.Value,
                x.Process.EmailId.Value,
                x.Request.InvalidatedAtUtc,
                x.Request.ConsumedAtUtc,
                x.Email.IsConfirmed))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
