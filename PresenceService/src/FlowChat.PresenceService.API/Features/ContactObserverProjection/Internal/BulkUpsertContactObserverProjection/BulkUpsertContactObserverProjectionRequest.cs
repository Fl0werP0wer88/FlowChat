using FlowChat.Core.Contracts;
using FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal;

namespace FlowChat.PresenceService.API.Features.ContactObserverProjection.Internal.BulkUpsertContactObserverProjection;

public sealed class BulkUpsertContactObserverProjectionRequest : IServiceInput
{
    public IReadOnlyCollection<ContactObserverProjectionRequest> Items { get; init; } = [];
}
