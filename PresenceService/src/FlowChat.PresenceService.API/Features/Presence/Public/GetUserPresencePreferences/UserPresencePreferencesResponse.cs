using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.PresenceService.API.Features.Presence.Public.GetUserPresencePreferences;

public sealed class UserPresencePreferencesResponse : IServiceOutput
{
    /// <summary>The user's saved manual preference. Null means no preference — Active is used on connect.</summary>
    public PresenceStatus? PreferredStatus { get; init; }
}
