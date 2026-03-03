using FlowChat.AuthService.Application.Models;
using FlowChat.AuthService.Domain.Entities;

namespace FlowChat.AuthService.Application.Contracts.Persistence;

public interface IIdentityRepository
{
    Task<Guid> CreateUserAsync(Identity user, string password, CancellationToken cancellationToken);
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken);
    Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken);
}
