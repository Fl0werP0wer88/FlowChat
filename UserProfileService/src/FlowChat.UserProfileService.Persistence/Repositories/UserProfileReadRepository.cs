using FlowChat.Shared.Domain;
using FlowChat.Shared.Persistance;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileReadRepository(AppDbContext dbContext)
    : ReadRepositoryBase<UserProfile, UserProfileDto>(dbContext), IUserProfileReadRepository
{
    private static readonly Expression<Func<UserProfile, UserProfileDto>> UserProfileDtoProjection = x => new(
        x.Id.Value,
        x.FriendlyUserId,
        x.AvatarUrl,
        x.Bio,
        x.IsActive,
        x.LastSeenAtUtc == null ? null : x.LastSeenAtUtc.Value,
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
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => x.FriendlyUserId)
            .Select(MapToDto)
            .ToListAsync(cancellationToken);
    }

    public async Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default)
    {
        var normalizedFriendlyUserId = NormalizeFriendlyUserId(friendlyUserId);

        return await Query
            .Where(x => x.NormalizedFriendlyUserId == normalizedFriendlyUserId)
            .Select(MapToDto)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmailAddress = EmailAddress.Create(emailAddress);

        return await DbContext.Set<Email>()
            .AnyAsync(x => x.Address == normalizedEmailAddress, cancellationToken);
    }

    public async Task<bool> FriendlyUserIdExistsAsync(
        string friendlyUserId,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedFriendlyUserId = NormalizeFriendlyUserId(friendlyUserId);

        return await DbContext.Set<UserProfile>()
            .AnyAsync(
                x => (!excludedUserId.HasValue || x.Id != Id<UserProfile>.FromGuid(excludedUserId.Value))
                     && x.NormalizedFriendlyUserId == normalizedFriendlyUserId,
                cancellationToken);
    }

    private static string NormalizeFriendlyUserId(string friendlyUserId) => friendlyUserId.Trim().ToLowerInvariant();
}

