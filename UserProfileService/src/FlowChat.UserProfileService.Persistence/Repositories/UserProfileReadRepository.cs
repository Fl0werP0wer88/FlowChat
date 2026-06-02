using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;
using FlowChat.UserProfileService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.UserProfileService.Persistence.Repositories;

public sealed class UserProfileReadRepository(AppDbContext dbContext) : IUserProfileReadRepository
{
    public async Task<UserProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Query()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity is null ? null : await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfileDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await Query()
            .ToListAsync(cancellationToken);

        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<IReadOnlyList<UserProfileDto>> SearchAsync(
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
            .ThenBy(entity => entity.UserName)
            .ToListAsync(cancellationToken);

        return await MapToDtosAsync(entities, cancellationToken);
    }

    public async Task<UserProfileDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (!EmailAddress.TryCreate(email, out var normalizedEmail))
        {
            return null;
        }

        var userProfileId = await dbContext.EmailReads
            .AsNoTracking()
            .Where(x => x.Address == normalizedEmail.Value)
            .Select(x => (Guid?)x.UserProfileId)
            .FirstOrDefaultAsync(cancellationToken);

        return userProfileId is null ? null : await GetByIdAsync(userProfileId.Value, cancellationToken);
    }

    public async Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default)
    {
        if (!FriendlyUserId.TryCreate(friendlyUserId, out var normalizedFriendlyUserId))
        {
            return null;
        }

        var entity = await Query()
            .FirstOrDefaultAsync(x => x.UserName == normalizedFriendlyUserId.Value, cancellationToken);

        return entity is null ? null : await MapToDtoAsync(entity, cancellationToken);
    }

    public async Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmailAddress = EmailAddress.Create(emailAddress);

        return await dbContext.EmailReads
            .AsNoTracking()
            .AnyAsync(x => x.Address == normalizedEmailAddress.Value, cancellationToken);
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

        return await Query()
            .AnyAsync(
                x => (!excludedUserId.HasValue || x.Id != excludedUserId.Value)
                     && x.UserName == normalizedFriendlyUserId.Value,
                cancellationToken);
    }

    private IQueryable<UserProfileReadEntity> Query()
    {
        return dbContext.UserProfileReads.AsNoTracking();
    }

    private async Task<IReadOnlyList<UserProfileDto>> MapToDtosAsync(
        IReadOnlyCollection<UserProfileReadEntity> entities,
        CancellationToken cancellationToken)
    {
        var ids = entities.Select(x => x.Id).ToArray();
        var emails = await LoadEmailsAsync(ids, cancellationToken);
        var phones = await LoadPhonesAsync(ids, cancellationToken);

        return entities
            .Select(entity => MapToDto(
                entity,
                emails.GetValueOrDefault(entity.Id, []),
                phones.GetValueOrDefault(entity.Id, [])))
            .ToList();
    }

    private async Task<UserProfileDto> MapToDtoAsync(
        UserProfileReadEntity entity,
        CancellationToken cancellationToken)
    {
        var emails = await LoadEmailsAsync([entity.Id], cancellationToken);
        var phones = await LoadPhonesAsync([entity.Id], cancellationToken);

        return MapToDto(
            entity,
            emails.GetValueOrDefault(entity.Id, []),
            phones.GetValueOrDefault(entity.Id, []));
    }

    private async Task<Dictionary<Guid, IReadOnlyList<EmailDto>>> LoadEmailsAsync(
        IReadOnlyCollection<Guid> userProfileIds,
        CancellationToken cancellationToken)
    {
        return (await dbContext.EmailReads
                .AsNoTracking()
                .Where(email => userProfileIds.Contains(email.UserProfileId))
                .Select(email => new
                {
                    email.UserProfileId,
                    Dto = new EmailDto(
                        email.Id,
                        email.Address,
                        email.IsMain,
                        email.IsAuth,
                        email.IsConfirmed,
                        email.IsVisible)
                })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.UserProfileId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<EmailDto>)x.Select(email => email.Dto).ToList());
    }

    private async Task<Dictionary<Guid, IReadOnlyList<PhoneDto>>> LoadPhonesAsync(
        IReadOnlyCollection<Guid> userProfileIds,
        CancellationToken cancellationToken)
    {
        return (await dbContext.PhoneReads
                .AsNoTracking()
                .Where(phone => userProfileIds.Contains(phone.UserProfileId))
                .Select(phone => new
                {
                    phone.UserProfileId,
                    Dto = new PhoneDto(
                        phone.Id,
                        phone.Number,
                        phone.IsMain,
                        phone.IsConfirmed,
                        phone.IsVisible)
                })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.UserProfileId)
            .ToDictionary(x => x.Key, x => (IReadOnlyList<PhoneDto>)x.Select(phone => phone.Dto).ToList());
    }

    private static UserProfileDto MapToDto(
        UserProfileReadEntity entity,
        IReadOnlyList<EmailDto> emails,
        IReadOnlyList<PhoneDto> phones)
    {
        return new UserProfileDto(
            entity.Id,
            entity.UserName,
            entity.FirstName,
            entity.LastName,
            entity.Organization,
            entity.AvatarUrl,
            entity.Bio,
            entity.IsActive,
            entity.LastSeenAtUtc,
            emails,
            phones);
    }
}
