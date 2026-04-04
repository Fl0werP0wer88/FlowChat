using FlowChat.AuthService.Domain.Entities.Account;

namespace FlowChat.AuthService.Application.Contracts.Persistence;

public interface IAccountRepository
{
    Task CreateAsync(Account account, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(Guid accountId, CancellationToken cancellationToken);
    Task<Account?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken);
    Task<Account?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken);
    Task<Account?> GetByLoginAsync(string login, CancellationToken cancellationToken);
    Task UpdateAsync(Account account, CancellationToken cancellationToken);
}
