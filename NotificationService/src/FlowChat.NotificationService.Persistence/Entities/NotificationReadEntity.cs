using FlowChat.NotificationService.Domain.Enums;
using FlowChat.Shared.Persistance;

namespace FlowChat.NotificationService.Persistence.Entities;

public sealed class NotificationReadEntity : ReadEntityBase
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public NotificationType Type { get; init; }
    public NotificationStatus Status { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? FailureReason { get; init; }
    public string? SourceMessageKey { get; init; }
    public DateTimeOffset? SentAtUtc { get; init; }
    public DateTimeOffset CreatedAtUtc { get; init; }
}
