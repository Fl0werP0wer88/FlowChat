using FlowChat.ChatService.Application.Features.UserProfile;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionWriteRepository
{
    Task InsertAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(UserProfileProjectionDto projection, CancellationToken cancellationToken = default);
}
