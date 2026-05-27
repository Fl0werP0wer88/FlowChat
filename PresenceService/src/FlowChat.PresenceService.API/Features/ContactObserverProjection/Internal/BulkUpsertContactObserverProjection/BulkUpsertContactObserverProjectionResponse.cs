using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertContactObserverProjection;

public sealed record BulkUpsertContactObserverProjectionResponse(
    int RequestedCount,
    int UpsertedCount) : IServiceOutput;
