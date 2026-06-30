using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.NotificationService.Domain.Enums;
using UserProfileMarker = FlowChat.NotificationService.Domain.Entities.UserProfiles.UserProfile;

namespace FlowChat.NotificationService.Domain.Entities.Notification;

public sealed class Notification : AggregateRootBase<Notification>
{
    private Notification() : base(Id<Notification>.New())
    {
    }

    private Notification(
        Id<Notification> id,
        Id<UserProfileMarker> userId,
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

    public Id<UserProfileMarker> UserId { get; private set; } = null!;
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
        Id<Notification> id,
        Id<UserProfileMarker> userId,
        EmailAddress email,
        string displayName,
        string body,
        string? sourceMessageKey)
    {
        return Create(
            id,
            userId,
            email,
            displayName,
            body,
            NotificationType.Welcome,
            sourceMessageKey);
    }

    public static Notification CreateEmailVerification(
        Id<Notification> id,
        Id<UserProfileMarker> userId,
        EmailAddress email,
        string displayName,
        string body,
        string? sourceMessageKey)
    {
        return Create(
            id,
            userId,
            email,
            displayName,
            body,
            NotificationType.EmailVerification,
            sourceMessageKey);
    }

    private static Notification Create(
        Id<Notification> id,
        Id<UserProfileMarker> userId,
        EmailAddress email,
        string displayName,
        string body,
        NotificationType type,
        string? sourceMessageKey)
    {
        ArgumentNullException.ThrowIfNull(id);

        ArgumentNullException.ThrowIfNull(userId);

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
            id,
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

