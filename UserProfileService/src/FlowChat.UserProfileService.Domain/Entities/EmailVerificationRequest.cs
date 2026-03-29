using FlowChat.Shared.Domain;

namespace FlowChat.UserProfileService.Domain.Entities;

public sealed class EmailVerificationRequest : AggregateRootBase<EmailVerificationRequest>
{
    private EmailVerificationRequest() : base(null)
    {
    }

    private EmailVerificationRequest(
        Id<EmailVerificationRequest>? id,
        Id<UserProfile> userProfileId,
        Id<Email> emailId,
        string nonce,
        DateTime expiresAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(emailId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        if (expiresAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("Expiration time must be in UTC.");
        }

        UserProfileId = userProfileId;
        EmailId = emailId;
        Nonce = nonce.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public Id<UserProfile> UserProfileId { get; private set; }
    public Id<Email> EmailId { get; private set; }
    public string Nonce { get; private set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; private set; }
    public DateTime? InvalidatedAtUtc { get; private set; }
    public DateTime? ConsumedAtUtc { get; private set; }

    public static EmailVerificationRequest Create(
        Id<UserProfile> userProfileId,
        Id<Email> emailId,
        string nonce,
        DateTime expiresAtUtc,
        Id<EmailVerificationRequest>? id = null)
    {
        return new EmailVerificationRequest(id, userProfileId, emailId, nonce, expiresAtUtc);
    }

    public bool IsExpired(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return ExpiresAtUtc <= utcNow;
    }

    public bool IsActive(DateTime utcNow)
    {
        EnsureUtc(utcNow);
        return ConsumedAtUtc is null && InvalidatedAtUtc is null && !IsExpired(utcNow);
    }

    public void Invalidate(DateTime utcNow)
    {
        EnsureUtc(utcNow);

        if (InvalidatedAtUtc is not null || ConsumedAtUtc is not null)
        {
            return;
        }

        InvalidatedAtUtc = utcNow;
    }

    public void Consume(DateTime utcNow)
    {
        EnsureUtc(utcNow);

        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTime value)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new InvalidOperationException("DateTime value must be in UTC.");
        }
    }
}
