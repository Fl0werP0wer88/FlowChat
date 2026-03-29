using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfiles.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase<UserProfile, UserProfileDto>(dbContext), IUserProfileReadRepository
{
    private static readonly Expression<Func<UserProfile, UserProfileDto>> UserProfileDtoProjection = x => new(
        x.Id.Value,
        x.UserName,
        x.DisplayName,
        x.AvatarUrl,
        x.Bio,
        x.IsActive,
        x.LastSeenAtUtc,
        x.Emails
            .Select(email => new EmailDto(
                email.Id.Value,
                email.Address.Value,
                email.IsMain,
                email.IsAuth,
                email.IsConfirmed))
            .ToList(),
        x.Phones
            .Select(phone => new PhoneDto(
                phone.Id.Value,
                phone.Number.Value,
                phone.IsMain))
            .ToList());

    protected override Expression<Func<UserProfile, UserProfileDto>> MapToDto => UserProfileDtoProjection;

    public async Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Query
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.UserName)
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = NormalizeUserName(userName);

        return await Query
            .Where(x => x.NormalizedUserName == normalizedUserName)
            .Select(MapToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmailAddress = EmailAddress.Create(emailAddress);

        return await DbContext.Set<Email>()
            .AnyAsync(x => x.Address == normalizedEmailAddress, cancellationToken);
    }

    public async Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = NormalizeUserName(userName);

        return await DbContext.Set<UserProfile>()
            .AnyAsync(
                x => (!excludedUserId.HasValue || x.Id != Id<UserProfile>.FromGuid(excludedUserId.Value))
                     && x.NormalizedUserName == normalizedUserName,
                cancellationToken);
    }

    private static string NormalizeUserName(string userName) => userName.Trim().ToLowerInvariant();
}

