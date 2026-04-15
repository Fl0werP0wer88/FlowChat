using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Realtime;

public sealed class PresencePreferencesDto
{
    public PresenceStatus? PreferredStatus { get; init; }
}
