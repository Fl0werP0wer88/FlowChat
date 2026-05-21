using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.GetUserProfile;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.SearchUserProfiles;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileReadRepository : IReadRepository<UserProfileDto>
{
    Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchUserProfileDto>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default);
    Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default);
    Task<bool> FriendlyUserIdExistsAsync(
        string friendlyUserId,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);
}

