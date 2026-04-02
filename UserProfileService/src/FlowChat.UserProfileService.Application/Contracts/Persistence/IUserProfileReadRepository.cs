using FlowChat.Shared.Application;
using FlowChat.UserProfileService.Application.Features.UserProfile.Queries.GetUserProfile;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileReadRepository : IReadRepository<UserProfileDto>
{
    Task<IReadOnlyList<UserProfileDto>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<bool> EmailAddressExistsAsync(string emailAddress, CancellationToken cancellationToken = default);
    Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);
}

