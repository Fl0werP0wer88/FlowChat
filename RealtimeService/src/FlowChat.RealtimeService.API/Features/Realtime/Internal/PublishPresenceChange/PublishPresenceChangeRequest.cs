using FlowChat.Core.Contracts;
using FlowChat.Core.Domain;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;

public sealed class PublishPresenceChangeRequest : IServiceInput
{
    public Guid UserId { get; init; }
    public UserStatus Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
