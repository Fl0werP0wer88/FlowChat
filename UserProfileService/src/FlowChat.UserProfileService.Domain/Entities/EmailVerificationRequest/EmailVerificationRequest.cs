using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Domain.Entities.UserProfile;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

public sealed class EmailVerificationRequest : AggregateRootBase<EmailVerificationRequest>
{
    private EmailVerificationRequest() : base(Id<EmailVerificationRequest>.New())
    {
    }

    private EmailVerificationRequest(
        Id<EmailVerificationRequest> id,
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId,
        string nonce,
        UtcDateTimeOffset expiresAtUtc) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(emailId);
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        UserProfileId = userProfileId;
        EmailId = emailId;
        Nonce = nonce.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public Id<DomainUserProfile> UserProfileId { get; private set; } = default!;
    public Id<DomainEmail> EmailId { get; private set; } = default!;
    public string Nonce { get; private set; } = string.Empty;
    public UtcDateTimeOffset ExpiresAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
    public UtcDateTimeOffset? InvalidatedAtUtc { get; private set; }
    public UtcDateTimeOffset? ConsumedAtUtc { get; private set; }

    public static EmailVerificationRequest Create(
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId,
        string nonce,
        UtcDateTimeOffset expiresAtUtc,
        Id<EmailVerificationRequest>? id = null)
    {
        return new EmailVerificationRequest(id ?? Id<EmailVerificationRequest>.New(), userProfileId, emailId, nonce, expiresAtUtc);
    }

    public bool IsExpired(UtcDateTimeOffset utcNow)
    {
        return ExpiresAtUtc <= utcNow;
    }

    // A request is only usable when none of the three terminal states have been reached:
    // consumed (successfully used), invalidated (explicitly cancelled), or expired (time-based).
    public bool IsActive(UtcDateTimeOffset utcNow)
    {
        return ConsumedAtUtc is null && InvalidatedAtUtc is null && !IsExpired(utcNow);
    }

    public void Invalidate(UtcDateTimeOffset utcNow)
    {
        // Idempotent — also blocks invalidating an already-consumed request to prevent
        // overwriting ConsumedAtUtc with a later timestamp.
        if (InvalidatedAtUtc is not null || ConsumedAtUtc is not null)
        {
            return;
        }

        InvalidatedAtUtc = utcNow;
    }

    public void Consume(UtcDateTimeOffset utcNow)
    {
        // Idempotent — email confirmation links may be followed more than once.
        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = utcNow;
    }

}
