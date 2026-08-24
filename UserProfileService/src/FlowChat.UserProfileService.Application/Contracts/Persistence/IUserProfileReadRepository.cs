using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.UserProfile.Model;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileReadRepository : IReadRepository<UserProfileDto>
{
    Task<IReadOnlyList<UserProfileDto>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfileDto>> SearchAsync(
        string? firstName,
        string? lastName,
        string? organization,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserProfileSearchResultDto>> SearchRangeAscendingAsync(
        string? firstName,
        string? lastName,
        string? organization,
        string? cursor,
        int limit,
        CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetByFriendlyUserIdAsync(string friendlyUserId, CancellationToken cancellationToken = default);
    Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default);
    Task<bool> FriendlyUserIdExistsAsync(
        string friendlyUserId,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);
}

