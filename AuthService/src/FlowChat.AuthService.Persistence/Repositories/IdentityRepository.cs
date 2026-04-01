using System.Globalization;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Models;
using FlowChat.AuthService.Domain.Entities;
using FlowChat.AuthService.Persistence.Identity;
using Microsoft.AspNetCore.Identity;

namespace FlowChat.AuthService.Persistence.Repositories;

public class IdentityRepository : IIdentityRepository
{
    private const string LoginProvider = "FlowChat";
    private const string RefreshTokenName = "RefreshToken";
    private const string RefreshTokenExpiryName = "RefreshTokenExpiry";

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

    public async Task<Domain.Entities.Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null
            ? null
            : MapToDomainIdentity(user);
    }

    public async Task<Domain.Entities.Identity?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(emailAddress))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(emailAddress.Trim());
        return user is null
            ? null
            : MapToDomainIdentity(user);
    }

    private static UserEntity MapToIdentityUser(Domain.Entities.Identity domainUser)
    {
        var user = new UserEntity
        {
            Id = domainUser.Id,
            UserName = domainUser.UserName,
            Email = domainUser.Email,
            PhoneNumber = domainUser.PhoneNumber,
            EmailConfirmed = domainUser.EmailConfirmed
        };

        return user;
    }

    private static Domain.Entities.Identity MapToDomainIdentity(UserEntity user)
    {
        return Domain.Entities.Identity.Restore(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed);
    }

    public async Task UpdateAsync(Domain.Entities.Identity domainUser, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(domainUser);

        var user = await _userManager.FindByIdAsync(domainUser.Id.Value.ToString());
        if (user is null)
        {
            throw new InvalidOperationException($"User with id '{domainUser.Id.Value}' was not found.");
        }

        user.UserName = domainUser.UserName;
        user.Email = domainUser.Email;
        user.PhoneNumber = domainUser.PhoneNumber;
        user.EmailConfirmed = domainUser.EmailConfirmed;
        user.PhoneNumberConfirmed = domainUser.PhoneNumberConfirmed;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => $"{e.Code}: {e.Description}"));
            throw new InvalidOperationException($"User update failed: {errors}");
        }
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

    public async Task<AuthenticatedUser?> GetAuthenticatedUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.EmailConfirmed)
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

    public async Task SaveRefreshTokenAsync(Guid userId, string token, DateTime expiresAtUtc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new InvalidOperationException($"User with id '{userId}' was not found.");

        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName, token);
        await _userManager.SetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenExpiryName,
            expiresAtUtc.ToString("O", CultureInfo.InvariantCulture));
    }

    public async Task<(string Token, DateTime ExpiresAtUtc)?> GetRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var token = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);
        var expiryString = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenExpiryName);

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(expiryString))
        {
            return null;
        }

        if (!DateTime.TryParse(expiryString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAtUtc))
        {
            return null;
        }

        return (token, expiresAtUtc);
    }

    public async Task RevokeRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return;
        }

        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, RefreshTokenExpiryName);
    }
}
