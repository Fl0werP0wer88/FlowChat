using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileReadRepository(AppDbContext dbContext) : IUserProfileReadRepository
{
    private const string NormalizedFriendlyUserIdPropertyName = "NormalizedFriendlyUserId";
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<UserProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var typedId = Id<UserProfile>.FromGuid(id);
        var entity = await Query()
            .FirstOrDefaultAsync(x => x.Id == typedId, cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await Query()
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var entities = await Query()
            .Where(x => x.IsActive)
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .ThenBy(x => EF.Property<string>(x, nameof(UserProfile.FriendlyUserId)))
            .ToListAsync(cancellationToken);

        return entities.Select(MapToDto).ToList();
    }

    public async Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default)
    {
        if (!FriendlyUserId.TryCreate(friendlyUserId, out var normalizedFriendlyUserId))
        {
            return null;
        }

        var entity = await Query()
            .FirstOrDefaultAsync(
                x => EF.Property<string>(x, NormalizedFriendlyUserIdPropertyName) == normalizedFriendlyUserId.Value,
                cancellationToken);

        return entity is null ? null : MapToDto(entity);
    }

    public async Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmailAddress = EmailAddress.Create(emailAddress);

        return await _dbContext.Set<Email>()
            .AnyAsync(x => x.Address == normalizedEmailAddress, cancellationToken);
    }

    public async Task<bool> FriendlyUserIdExistsAsync(
        string friendlyUserId,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        if (!FriendlyUserId.TryCreate(friendlyUserId, out var normalizedFriendlyUserId))
        {
            return false;
        }

        return await _dbContext.Set<UserProfile>()
            .AsNoTracking()
            .AnyAsync(
                x => (!excludedUserId.HasValue || x.Id != Id<UserProfile>.FromGuid(excludedUserId.Value))
                     && EF.Property<string>(x, NormalizedFriendlyUserIdPropertyName) == normalizedFriendlyUserId.Value,
                cancellationToken);
    }

    private IQueryable<UserProfile> Query()
    {
        return _dbContext.Set<UserProfile>()
            .AsNoTracking()
            .Include(x => x.Emails)
            .Include(x => x.Phones);
    }

    private static UserProfileDto MapToDto(UserProfile entity)
    {
        return new UserProfileDto(
            entity.Id.Value,
            entity.FriendlyUserId.Value,
            entity.AvatarUrl,
            entity.Bio,
            entity.IsActive,
            entity.LastSeenAtUtc?.Value,
            entity.Emails
                .Select(email => new EmailDto(
                    email.Id.Value,
                    email.Address.Value,
                    email.IsMain,
                    email.IsAuth,
                    email.IsConfirmed))
                .ToList(),
            entity.Phones
                .Select(phone => new PhoneDto(
                    phone.Id.Value,
                    phone.Number.Value,
                    phone.IsMain))
                .ToList());
    }
}
