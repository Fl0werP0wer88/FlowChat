using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Contracts.Persistence;

public interface IUserProfileRepository : IAsyncRepository<UserProfile>
{
    Task<IReadOnlyList<UserProfile>> GetActiveAsync(CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<UserProfile?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> UserNameExistsAsync(
        string userName,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);
}
