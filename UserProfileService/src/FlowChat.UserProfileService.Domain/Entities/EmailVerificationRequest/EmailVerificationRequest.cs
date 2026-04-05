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

    public bool IsActive(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        return ConsumedAtUtc is null && InvalidatedAtUtc is null && !IsExpired(utcNow);
    }

    public void Invalidate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

        if (InvalidatedAtUtc is not null || ConsumedAtUtc is not null)
        {
            return;
        }

        InvalidatedAtUtc = utcNow;
    }

    public void Consume(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

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
