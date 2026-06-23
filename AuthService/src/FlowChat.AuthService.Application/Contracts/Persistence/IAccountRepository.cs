using FlowChat.AuthService.Domain.Entities.Account;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.AuthService.Application.Contracts.Persistence;

public interface IAccountRepository
{
    Task CreateAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(Id<Account> accountId, CancellationToken cancellationToken);
    Task<Account?> GetByEmailAsync(EmailAddress emailAddress, CancellationToken cancellationToken);
    Task<Account?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken);
    Task<Account?> GetByLoginAsync(string login, CancellationToken cancellationToken);
    Task UpdateAsync(Account account, CancellationToken cancellationToken);
}
