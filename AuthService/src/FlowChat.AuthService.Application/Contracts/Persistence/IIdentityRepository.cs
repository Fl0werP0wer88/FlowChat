using FlowChat.AuthService.Application.Commands;
using FlowChat.AuthService.Application.Models;

namespace FlowChat.AuthService.Application.Contracts.Persistence;

public interface IIdentityRepository
{
    Task<Guid> CreateUserAsync(RegisterUserCommand command, CancellationToken cancellationToken);
    Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken);
    Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken);
}
