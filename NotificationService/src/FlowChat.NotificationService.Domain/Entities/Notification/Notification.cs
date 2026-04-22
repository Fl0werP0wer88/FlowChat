using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.NotificationService.Domain.Enums;

namespace FlowChat.NotificationService.Domain.Entities.Notification;

public sealed class Notification : AggregateRootBase<Notification>
{
    private Notification() : base(Id<Notification>.New())
    {
    }

    private Notification(
        Id<Notification> id,
        Guid userId,
        EmailAddress email,
        string displayName,
        string body,
        NotificationType type,
        string? sourceMessageKey) : base(id)
    {
        UserId = userId;
        Email = email;
        DisplayName = displayName;
        Body = body;
        Type = type;
        Status = NotificationStatus.Pending;
        SourceMessageKey = sourceMessageKey;
    }

    public Guid UserId { get; private set; }
    public EmailAddress Email { get; private set; } = null!;
    public string DisplayName { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? FailureReason { get; private set; }
    public string? SourceMessageKey { get; private set; }
    public UtcDateTimeOffset? SentAtUtc { get; private set; }

    public static Notification CreateWelcome(
        Guid userId,
        EmailAddress email,
        string displayName,
        string body,
        string? sourceMessageKey)
    {
        return Create(
            userId,
            email,
            displayName,
            body,
            NotificationType.Welcome,
            sourceMessageKey);
    }

    public static Notification CreateEmailVerification(
        Guid userId,
        EmailAddress email,
        string displayName,
        string body,
        string? sourceMessageKey)
    {
        return Create(
            userId,
            email,
            displayName,
            body,
            NotificationType.EmailVerification,
            sourceMessageKey);
    }

    private static Notification Create(
        Guid userId,
        EmailAddress email,
        string displayName,
        string body,
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

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new InvalidOperationException("Body is required.");
        }

        return new Notification(
            Id<Notification>.New(),
            userId,
            email,
            displayName.Trim(),
            body.Trim(),
            type,
            string.IsNullOrWhiteSpace(sourceMessageKey) ? null : sourceMessageKey.Trim());
    }

    public void MarkSent(string? providerMessageId)
    {
        Status = NotificationStatus.Sent;
        ProviderMessageId = string.IsNullOrWhiteSpace(providerMessageId) ? null : providerMessageId.Trim();
        FailureReason = null;
        SentAtUtc = UtcDateTimeOffset.UtcNow;
    }

    public void MarkFailed(string? failureReason)
    {
        Status = NotificationStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(failureReason) ? "Unknown notification error." : failureReason.Trim();
        ProviderMessageId = null;
        SentAtUtc = null;
    }
}

