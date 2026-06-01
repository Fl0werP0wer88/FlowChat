using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.Shared.Application;
using MediatR;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionBulkRepository
{
    Task<FlowChatResult<Unit>> BulkUpsertOrDeleteAsync(
        IReadOnlyCollection<UserProfileProjectionCommandItem> items,
        CancellationToken cancellationToken);
}
