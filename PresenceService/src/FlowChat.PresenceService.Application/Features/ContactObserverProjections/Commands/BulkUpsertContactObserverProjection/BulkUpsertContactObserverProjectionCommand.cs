using FlowChat.Shared.Application;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections.Commands.BulkUpsertContactObserverProjection;

public sealed record BulkUpsertContactObserverProjectionCommand(
    IReadOnlyCollection<BulkUpsertContactObserverProjectionCommandItem> Items)
    : IBulkUpsertCommand<BulkUpsertContactObserverProjectionCommandItem>;

public sealed record BulkUpsertContactObserverProjectionCommandItem(
    Guid ObservedUserId,
    Guid ObserverUserId);
