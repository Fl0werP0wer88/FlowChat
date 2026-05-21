using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileReadRepository(AppDbContext dbContext) : IUserProfileReadRepository
{
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

    public async Task<IReadOnlyList<SearchUserProfileDto>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (!string.IsNullOrWhiteSpace(firstName))
        {
            var firstNamePattern = $"{firstName}%";
            query = query.Where(entity => entity.FirstName != null && EF.Functions.Like(entity.FirstName, firstNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(lastName))
        {
            var lastNamePattern = $"{lastName}%";
            query = query.Where(entity => entity.LastName != null && EF.Functions.Like(entity.LastName, lastNamePattern));
        }

        if (!string.IsNullOrWhiteSpace(organization))
        {
            var organizationPattern = $"{organization}%";
            query = query.Where(entity => entity.Organization != null && EF.Functions.Like(entity.Organization, organizationPattern));
        }

        var entities = await query
            .OrderBy(entity => entity.LastName ?? string.Empty)
            .ThenBy(entity => entity.FirstName ?? string.Empty)
            .ThenBy(entity => EF.Property<string>(entity, nameof(UserProfile.FriendlyUserId)))
            .ToListAsync(cancellationToken);

        return entities.Select(MapToSearchDto).ToList();
    }

    public async Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default)
    {
        if (!FriendlyUserId.TryCreate(friendlyUserId, out var normalizedFriendlyUserId))
        {
            return null;
        }

        var entity = await Query()
            .FirstOrDefaultAsync(
                x => x.FriendlyUserId == normalizedFriendlyUserId,
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
                     && x.FriendlyUserId == normalizedFriendlyUserId,
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

    private static SearchUserProfileDto MapToSearchDto(UserProfile entity)
    {
        var mainEmail = entity.Emails.FirstOrDefault(email => email.IsMain);
        var mainPhone = entity.Phones.FirstOrDefault(phone => phone.IsMain);

        return new SearchUserProfileDto
        {
            UserProfileId = entity.Id.Value,
            FriendlyUserId = entity.FriendlyUserId.Value,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            Organization = entity.Organization,
            MainEmail = mainEmail is null
                ? null
                : new SearchUserProfileEmailDto
                {
                    Address = mainEmail.Address.Value,
                    IsConfirmed = mainEmail.IsConfirmed,
                    IsVisible = mainEmail.IsVisible
                },
            MainPhone = mainPhone is null
                ? null
                : new SearchUserProfilePhoneDto
                {
                    Number = mainPhone.Number.Value,
                    IsConfirmed = mainPhone.IsConfirmed,
                    IsVisible = mainPhone.IsVisible
                },
            AvatarUrl = entity.AvatarUrl,
            Bio = entity.Bio,
            IsActive = entity.IsActive,
            LastSeenAtUtc = entity.LastSeenAtUtc?.Value
        };
    }
}
