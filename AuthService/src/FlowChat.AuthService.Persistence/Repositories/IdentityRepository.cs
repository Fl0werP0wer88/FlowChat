using System.Text.Json;
using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.User.Models;
using DomainIdentity = FlowChat.AuthService.Domain.Entities.Identity.Identity;
using UserEntity = FlowChat.AuthService.Persistence.Identity.UserEntity;
using Microsoft.AspNetCore.Identity;

namespace FlowChat.AuthService.Persistence.Repositories;

public class IdentityRepository : IIdentityRepository
{
    private const string LoginProvider = "FlowChat";
    private const string RefreshTokenName = "RefreshToken";
    private const string LegacyRefreshTokenExpiryName = "RefreshTokenExpiry";

    private static readonly JsonSerializerOptions RefreshTokenSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly UserManager<UserEntity> _userManager;

    public IdentityRepository(UserManager<UserEntity> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Guid> CreateUserAsync(DomainIdentity domainUser, string password, CancellationToken cancellationToken)
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

    public async Task<DomainIdentity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user is null
            ? null
            : MapToDomainIdentity(user);
    }

    public async Task<DomainIdentity?> GetByEmailAsync(string emailAddress, CancellationToken cancellationToken)
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

    private static UserEntity MapToIdentityUser(DomainIdentity domainUser)
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

    private static DomainIdentity MapToDomainIdentity(UserEntity user)
    {
        return DomainIdentity.Restore(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email,
            user.PhoneNumber,
            user.EmailConfirmed,
            user.PhoneNumberConfirmed);
    }

    public async Task UpdateAsync(DomainIdentity domainUser, CancellationToken cancellationToken)
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

    public async Task<AuthenticatedUser?> LoginUserAsync(
        string login,
        string password,
        string refreshToken,
        DateTime refreshTokenExpiresAtUtc,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(login)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(refreshToken))
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

        await SaveRefreshTokenAsync(user, refreshToken, refreshTokenExpiresAtUtc);

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

        await SaveRefreshTokenAsync(user, token, expiresAtUtc);
    }

    private async Task SaveRefreshTokenAsync(UserEntity user, string token, DateTime expiresAtUtc)
    {
        var payload = new RefreshTokenPayload(token, NormalizeUtc(expiresAtUtc));

        await _userManager.SetAuthenticationTokenAsync(
            user,
            LoginProvider,
            RefreshTokenName,
            JsonSerializer.Serialize(payload, RefreshTokenSerializerOptions));
    }

    public async Task<(string Token, DateTime ExpiresAtUtc)?> GetRefreshTokenAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return null;
        }

        var payloadJson = await _userManager.GetAuthenticationTokenAsync(user, LoginProvider, RefreshTokenName);
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<RefreshTokenPayload>(payloadJson, RefreshTokenSerializerOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.Token))
            {
                return null;
            }

            return (payload.Token, NormalizeUtc(payload.ExpiresAtUtc));
        }
        catch (JsonException)
        {
            return null;
        }
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
        await _userManager.RemoveAuthenticationTokenAsync(user, LoginProvider, LegacyRefreshTokenExpiryName);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value.ToUniversalTime()
        };
    }

    private sealed record RefreshTokenPayload(string Token, DateTime ExpiresAtUtc);
}
