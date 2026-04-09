using FlowChat.Core.Contracts;

namespace FlowChat.RealtimeService.Api.Features.Realtime.Internal.PublishPresenceChange;

public sealed class PublishPresenceChangeRequest : IServiceInput
{
    public Guid UserId { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset ChangedAtUtc { get; init; }
    public IReadOnlyCollection<Guid> RecipientUserIds { get; init; } = [];
}
