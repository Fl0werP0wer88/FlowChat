using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Domain.Entities.Notification;

public sealed class Notification : AggregateRootBase<Notification>
{
    private Notification() : base(null)
    {
    }

    private Notification(
        Id<Notification>? id,
        Guid userId,
        EmailAddress email,
        string displayName,
        NotificationType type,
        string? sourceMessageKey) : base(id)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Type = type;
        Status = NotificationStatus.Pending;
        SourceMessageKey = sourceMessageKey;
    }

    public Guid UserId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? SourceMessageKey { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public static Notification CreateWelcome(
        Guid userId,
        EmailAddress email,
        string displayName,
        string? sourceMessageKey)
    {
        return Create(
            userId,
            email,
            displayName,
            NotificationType.Welcome,
            sourceMessageKey);
    }

    public static Notification CreateEmailVerification(
        Guid userId,
        EmailAddress email,
        string displayName,
        string? sourceMessageKey)
    {
        return Create(
            userId,
            email,
            displayName,
            NotificationType.EmailVerification,
            sourceMessageKey);
    }

    private static Notification Create(
        Guid userId,
        EmailAddress email,
        string displayName,
        NotificationType type,
        string? sourceMessageKey)
    {
        if (userId == Guid.Empty)
        {
            throw new InvalidOperationException("UserId is required.");
        }

        ArgumentNullException.ThrowIfNull(email);

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidOperationException("DisplayName is required.");
        }

        return new Notification(
            null,
            userId,
            email,
            displayName.Trim(),
            type,
            string.IsNullOrWhiteSpace(sourceMessageKey) ? null : sourceMessageKey.Trim());
    }

    public void MarkSent(string? providerMessageId)
    {
        Status = NotificationStatus.Sent;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        FailureReason = null;
        SentAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(string? failureReason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Unknown notification error." : failureReason.Trim();
        ProviderMessageId = null;
        SentAtUtc = null;
    }
}

