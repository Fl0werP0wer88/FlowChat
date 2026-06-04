using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Consumers.Presence.Contracts;

public sealed class UserContactProjectionRequest : IConsumerOutput
{
    public string Source { get; init; } = string.Empty;
}
