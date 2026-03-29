using FlowChat.AuthService.Application.Contracts.Persistence;
using FlowChat.AuthService.Application.Features.Users.Models;
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

    public async Task<Domain.Entities.Identity?> GetByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
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
        if (user is null)
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
