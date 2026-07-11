using FlowChat.Core.Contracts;

namespace FlowChat.PresenceService.Application.Features.ContactObserverProjections;

public sealed class ContactObserverProjectionDto : IDbReadResponse
{
    public Guid ObservedUserId { get; init; }
    public Guid ObserverUserId { get; init; }
    public bool IsBlocked { get; init; }
    public int SourceVersion { get; init; }
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset LastModifiedAtUtc { get; init; }
}
