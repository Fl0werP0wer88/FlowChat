using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;

namespace FlowChat.AuthService.Application.Contracts.Persistence;

public interface IIdentityRepository
{
    Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken);
    Task<Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> IsEmailConfirmationTokenValidAsync(Guid userId, string token, CancellationToken cancellationToken);
    Task UpdateAsync(Identity user, CancellationToken cancellationToken);
    Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken);
}
