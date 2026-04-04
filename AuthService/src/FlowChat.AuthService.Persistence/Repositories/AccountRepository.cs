using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.AuthService.Persistence.Entities;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.AuthService.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private readonly AppDbContext _dbContext;

    public AccountRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Account account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(account);

        await _dbContext.Accounts.AddAsync(MapToEntity(account), cancellationToken);
    }

    public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Account?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedEmail = NormalizeEmail(emailAddress);
        if (normalizedEmail is null)
        {
            return null;
        }

        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Account?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFriendlyUserId = NormalizeFriendlyUserId(friendlyUserId);
        if (normalizedFriendlyUserId is null)
        {
            return null;
        }

        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.NormalizedFriendlyUserId == normalizedFriendlyUserId, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<Account?> GetByLoginAsync(string login, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        var normalizedLogin = login.Trim();

        return await GetByEmailAsync(normalizedLogin, cancellationToken)
            ?? await GetByFriendlyUserIdAsync(normalizedLogin, cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(account);

        var entity = await _dbContext.Accounts.FirstOrDefaultAsync(x => x.Id == account.Id.Value, cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Account with id '{account.Id.Value}' was not found.");
        }

        entity.Email = account.Email;
        entity.NormalizedEmail = NormalizeRequired(account.Email.Value);
        entity.FriendlyUserId = account.FriendlyUserId;
        entity.NormalizedFriendlyUserId = NormalizeRequired(account.FriendlyUserId);
        entity.PasswordHash = account.PasswordHash;
        entity.SecurityStamp = account.SecurityStamp;
        entity.AccessFailedCount = account.AccessFailedCount;
        entity.IsEmailConfirmed = account.IsEmailConfirmed;
    }

    private static AccountEntity MapToEntity(Account account)
    {
        return new AccountEntity
        {
            Id = account.Id.Value,
            Email = account.Email,
            NormalizedEmail = NormalizeRequired(account.Email.Value),
            FriendlyUserId = account.FriendlyUserId,
            NormalizedFriendlyUserId = NormalizeRequired(account.FriendlyUserId),
            PasswordHash = account.PasswordHash,
            SecurityStamp = account.SecurityStamp,
            AccessFailedCount = account.AccessFailedCount,
            IsEmailConfirmed = account.IsEmailConfirmed
        };
    }

    private static Account MapToDomain(AccountEntity entity)
    {
        return Account.Restore(
            entity.Id,
            entity.FriendlyUserId,
            entity.Email,
            entity.PasswordHash,
            entity.SecurityStamp,
            entity.AccessFailedCount,
            entity.IsEmailConfirmed);
    }

    private static string? NormalizeEmail(string? emailAddress)
    {
        if (!EmailAddress.TryCreate(emailAddress, out var normalized))
        {
            return null;
        }

        return normalized.Value;
    }

    private static string? NormalizeFriendlyUserId(string? friendlyUserId)
    {
        return string.IsNullOrWhiteSpace(friendlyUserId)
            ? null
            : NormalizeRequired(friendlyUserId);
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
