using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using FlowChat.SocialGraphService.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace FlowChat.SocialGraphService.Persistence.Repositories;

public sealed class UserProfileReadModelRepository(AppDbContext dbContext) : IUserProfileReadModelRepository
{
    private const string ProjectionSource = "user-profile-events";
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    public async Task<bool> UpsertAsync(UserProfileReadModel readModel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(readModel);

        var entity = await _dbContext.UserProfileReadModels
            .FirstOrDefaultAsync(x => x.UserProfileId == readModel.UserProfileId, cancellationToken);

        if (entity is null)
        {
            var now = DateTimeOffset.UtcNow;

            await _dbContext.UserProfileReadModels.AddAsync(
                new UserProfileReadModelEntity
                {
                    UserProfileId = readModel.UserProfileId,
                    UserName = readModel.UserName,
                    DisplayName = readModel.DisplayName,
                    MainEmail = readModel.MainEmail,
                    MainPhone = readModel.MainPhone,
                    AvatarUrl = readModel.AvatarUrl,
                    Bio = readModel.Bio,
                    IsActive = readModel.IsActive,
                    LastSeenAtUtc = readModel.LastSeenAtUtc,
                    IsEmailVisible = readModel.IsEmailVisible,
                    IsPhoneVisible = readModel.IsPhoneVisible,
                    CreatedBy = ProjectionSource,
                    CreatedAtUtc = now,
                    LastModifiedBy = ProjectionSource,
                    LastModifiedAtUtc = now
                },
                cancellationToken);

            return true;
        }

        entity.UserName = readModel.UserName;
        entity.DisplayName = readModel.DisplayName;
        entity.MainEmail = readModel.MainEmail;
        entity.MainPhone = readModel.MainPhone;
        entity.AvatarUrl = readModel.AvatarUrl;
        entity.Bio = readModel.Bio;
        entity.IsActive = readModel.IsActive;
        entity.LastSeenAtUtc = readModel.LastSeenAtUtc;
        entity.IsEmailVisible = readModel.IsEmailVisible;
        entity.IsPhoneVisible = readModel.IsPhoneVisible;
        entity.LastModifiedBy = ProjectionSource;
        entity.LastModifiedAtUtc = DateTimeOffset.UtcNow;

        return false;
    }
}
