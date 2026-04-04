using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowChat.AuthService.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    private const string NormalizedEmailPropertyName = "NormalizedEmail";
    private const string NormalizedFriendlyUserIdPropertyName = "NormalizedFriendlyUserId";
    private readonly AppDbContext _dbContext;

    public AccountRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateAsync(Account account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(account);

        var entry = await _dbContext.Accounts.AddAsync(account, cancellationToken);
        SetNormalizedProperties(entry, account);
    }

    public async Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typedId = Id<Account>.FromGuid(accountId);
        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);

        return entity;
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
            .FirstOrDefaultAsync(
                x => EF.Property<string>(x, NormalizedEmailPropertyName) == normalizedEmail,
                cancellationToken);

        return entity;
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
            .FirstOrDefaultAsync(
                x => EF.Property<string>(x, NormalizedFriendlyUserIdPropertyName) == normalizedFriendlyUserId,
                cancellationToken);

        return entity;
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

        var localEntity = _dbContext.Accounts.Local.FirstOrDefault(x => x.Id == account.Id);
        if (localEntity is not null && !ReferenceEquals(localEntity, account))
        {
            var localEntry = _dbContext.Entry(localEntity);
            localEntry.CurrentValues.SetValues(account);
            SetNormalizedProperties(localEntry, account);
            return;
        }

        var exists = await _dbContext.Accounts
            .AsNoTracking()
            .AnyAsync(x => x.Id == account.Id, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException($"Account with id '{account.Id.Value}' was not found.");
        }

        _dbContext.Accounts.Attach(account);
        var entry = _dbContext.Entry(account);
        entry.State = EntityState.Modified;
        SetNormalizedProperties(entry, account);
    }

    private static void SetNormalizedProperties(EntityEntry<Account> entry, Account account)
    {
        entry.Property(NormalizedEmailPropertyName).CurrentValue = NormalizeRequired(account.Email.Value);
        entry.Property(NormalizedFriendlyUserIdPropertyName).CurrentValue = NormalizeRequired(account.FriendlyUserId);
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
