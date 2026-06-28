using FlowChat.Core.Messaging;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using MediatR;

namespace FlowChat.ChatService.Application.Features.UserProfile.Commands.BulkUpsertOrDeleteUserProfileProjection;

public sealed record BulkUpsertOrDeleteUserProfileProjectionCommand(
    IReadOnlyCollection<UserProfileProjectionCommandItem> Items)
    : IProjectionBulkCommand<UserProfileProjectionCommandItem>;

public sealed record UserProfileProjectionCommandItem(
    Id<UserProfileProjectionDto> EntityId,
    UserProfileProjectionDto? Value,
    OperationType Operation,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<UserProfileProjectionDto>;
