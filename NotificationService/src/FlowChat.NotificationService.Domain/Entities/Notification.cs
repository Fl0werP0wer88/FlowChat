using FlowChat.NotificationService.Domain.Common;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Domain.Entities;

public sealed class Notification : AuditableEntity
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid userId,
        string email,
        string displayName,
        NotificationType type,
        string? sourceMessageKey)
    {
        Id = id;
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Type = type;
        Status = NotificationStatus.Pending;
        SourceMessageKey = sourceMessageKey;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? SourceMessageKey { get; private set; }
    public DateTime? SentAtUtc { get; private set; }

    public static Notification CreateWelcome(
        Guid userId,
        string email,
        string displayName,
        string? sourceMessageKey)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("UserId is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("DisplayName is required.");
        }

        return new Notification(
            Guid.NewGuid(),
            userId,
            email.Trim(),
            displayName.Trim(),
            NotificationType.Welcome,
            string.IsNullOrWhiteSpace(sourceMessageKey) ? null : sourceMessageKey.Trim());
    }

    public void MarkSent(string? providerMessageId)
    {
        Status = NotificationStatus.Sent;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        FailureReason = null;
        SentAtUtc = DateTime.UtcNow;
    }

    public void MarkFailed(string? failureReason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Unknown notification error." : failureReason.Trim();
        ProviderMessageId = null;
        SentAtUtc = null;
    }
}
