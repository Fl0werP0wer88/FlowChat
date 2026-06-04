using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.SocialGraphService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed record BulkUpsertOrDeleteUserProfileProjectionCommand(
    IReadOnlyCollection<UserProfileProjectionCommandItem> Items)
    : IProjectionBulkCommand<UserProfileProjectionCommandItem>;

public sealed record UserProfileProjectionCommandItem(
    Id<UserProfileProjectionDto> EntityId,
    UserProfileProjectionDto? Value,
    int SourceVersion)
    : IProjectionCommandItem<UserProfileProjectionDto>;
