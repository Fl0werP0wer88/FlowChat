using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Presence.Contracts;

public sealed class BulkUpsertContactObserverProjectionRequest : IConsumerOutput
{
    public IReadOnlyCollection<ContactObserverProjectionRequest> Items { get; init; } = [];
}
