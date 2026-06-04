using FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;
using FlowChat.Shared.Application;

namespace FlowChat.ChatService.Application.Contracts.Persistence;

public interface IUserProfileProjectionBulkRepository
    : IProjectionBulkRepository<UserProfileProjectionCommandItem>;
