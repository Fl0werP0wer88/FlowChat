using FlowChat.Core.Messaging;
using MediatR;
using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertOrDeleteUserContactProjection;

public sealed record BulkUpsertOrDeleteUserContactProjectionCommand(
    IReadOnlyCollection<UserContactProjectionCommandItem> Items)
    : IProjectionBulkCommand<UserContactProjectionCommandItem>;

public sealed record UserContactProjectionCommandItem(
    Guid ObservedUserId,
    Guid ObserverUserId,
    ContactObserverProjectionDto? Value,
    OperationType Operation,
    int SourceVersion,
    DateTimeOffset SourceCreatedAtUtc,
    DateTimeOffset SourceLastModifiedAtUtc,
    DateTimeOffset? SourceDeletedAtUtc)
    : IProjectionCommandItem<ContactObserverProjectionDto>;
