using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

public sealed class EmailVerificationRequest : AggregateRootBase<EmailVerificationRequest>
{
    private EmailVerificationRequest() : base(null)
    {
    }

    private EmailVerificationRequest(
        Id<EmailVerificationRequest>? id,
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId,
        string nonce,
        DateTimeOffset expiresAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(emailId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        // Enforce UTC at construction to prevent subtle expiry bugs when comparing against DateTimeOffset.UtcNow.
        if (expiresAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Expiration time must be in UTC.");
        }

        UserProfileId = userProfileId;
        EmailId = emailId;
        Nonce = nonce.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public Id<DomainUserProfile> UserProfileId { get; private set; } = default!;
    public Id<DomainEmail> EmailId { get; private set; } = default!;
    public string Nonce { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? InvalidatedAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public static EmailVerificationRequest Create(
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId,
        string nonce,
        DateTimeOffset expiresAtUtc,
        Id<EmailVerificationRequest>? id = null)
    {
        return new EmailVerificationRequest(id, userProfileId, emailId, nonce, expiresAtUtc);
    }

    public bool IsExpired(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return ExpiresAtUtc <= utcNow;
    }

    // A request is only usable when none of the three terminal states have been reached:
    // consumed (successfully used), invalidated (explicitly cancelled), or expired (time-based).
    public bool IsActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return ConsumedAtUtc is null && InvalidatedAtUtc is null && !IsExpired(utcNow);
    }

    public void Invalidate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

        // Idempotent — also blocks invalidating an already-consumed request to prevent
        // overwriting ConsumedAtUtc with a later timestamp.
        if (InvalidatedAtUtc is not null || ConsumedAtUtc is not null)
        {
            return;
        }

        InvalidatedAtUtc = utcNow;
    }

    public void Consume(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

        // Idempotent — email confirmation links may be followed more than once.
        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = utcNow;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("DateTimeOffset value must be in UTC.");
        }
    }
}
