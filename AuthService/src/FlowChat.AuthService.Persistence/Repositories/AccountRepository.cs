using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FlowChat.AuthService.Persistence.Repositories;

public sealed class AccountRepository : IAccountRepository
{
    // EF shadow property keeps email lookups case-insensitive without leaking normalization into the domain model.
    private const string NormalizedEmailPropertyName = "NormalizedEmail";
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

    public async Task<Account?> GetByIdAsync(Id<Account> accountId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);
    }

    public async Task<Account?> GetByEmailAsync(EmailAddress emailAddress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(emailAddress);

        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => EF.Property<string>(x, NormalizedEmailPropertyName) == emailAddress.Value,
                cancellationToken);

        return entity;
    }

    public async Task<Account?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!FriendlyUserId.TryCreate(friendlyUserId, out var normalizedFriendlyUserId))
        {
            return null;
        }

        var entity = await _dbContext.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.FriendlyUserId == normalizedFriendlyUserId,
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
        // Try email first â€” if the input is a valid email address look it up by email;
        // fall back to friendlyUserId so users can log in with either identifier.
        if (EmailAddress.TryCreate(normalizedLogin, out var emailAddress))
        {
            var accountByEmail = await GetByEmailAsync(emailAddress, cancellationToken);
            if (accountByEmail is not null)
            {
                return accountByEmail;
            }
        }

        return await GetByFriendlyUserIdAsync(normalizedLogin, cancellationToken);
    }

    public async Task UpdateAsync(Account account, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(account);

        // If a different instance of the same entity is already tracked by EF, update its values
        // in place rather than attaching the new instance â€” attaching would throw an InvalidOperationException
        // ("another instance with the same key value is already being tracked").
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
    }

    private static string NormalizeRequired(string value)
    {
        return value.Trim().ToLowerInvariant();
    }
}
