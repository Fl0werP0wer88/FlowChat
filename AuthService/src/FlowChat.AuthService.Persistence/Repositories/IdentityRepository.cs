using FlowChat.AuthService.Application.Models;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace FlowChat.AuthService.Persistence.Repositories;

public class IdentityRepository : IIdentityRepository
{
    private readonly UserManager<UserEntity> _userManager;

    public IdentityRepository(UserManager<UserEntity> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid> CreateUserAsync(Domain.Entities.Identity domainUser, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(domainUser);

        var user = MapToIdentityUser(domainUser);

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"User creation failed: {errors}");
        }

        return user.Id;
    }

    private static UserEntity MapToIdentityUser(Domain.Entities.Identity domainUser)
    {
        var user = new UserEntity
        {
            Id = domainUser.Id,
            UserName = domainUser.UserName,
            Email = domainUser.Email,
            PhoneNumber = domainUser.PhoneNumber,
            EmailConfirmed = false
        };
        
        return user;
    }

    public async Task<string> GenerateEmailConfirmationTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            throw new InvalidOperationException($"User with id '{userId}' was not found.");
        }

        return await _userManager.GenerateEmailConfirmationTokenAsync(user);
    }

    public async Task<bool> ConfirmEmailAsync(Guid userId, string token, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return false;
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return result.Succeeded;
    }

    public async Task<AuthenticatedUser?> AuthenticateUserAsync(string login, string password, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(login) ?? await _userManager.FindByNameAsync(login);
        if (user is null || !user.EmailConfirmed)
        {
            return null;
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthenticatedUser
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToArray()
        };
    }
}
