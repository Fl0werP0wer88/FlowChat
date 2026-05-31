using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;

namespace FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;

public sealed class EmailVerificationRequest : EntityBase<EmailVerificationRequest>
{
    private EmailVerificationRequest() : base(Id<EmailVerificationRequest>.New())
    {
    }

    private EmailVerificationRequest(
        Id<EmailVerificationRequest> id,
        string nonce,
        UtcDateTimeOffset expiresAtUtc) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        Nonce = nonce.Trim();
        ExpiresAtUtc = expiresAtUtc;
    }

    public string Nonce { get; private set; } = string.Empty;
    public UtcDateTimeOffset ExpiresAtUtc { get; private set; } = UtcDateTimeOffset.UtcNow;
    public UtcDateTimeOffset? InvalidatedAtUtc { get; private set; }
    public UtcDateTimeOffset? ConsumedAtUtc { get; private set; }

    public static EmailVerificationRequest Create(
        Id<EmailVerificationRequest> id,
        string nonce,
        UtcDateTimeOffset expiresAtUtc)
    {
        ArgumentNullException.ThrowIfNull(id);
        return new EmailVerificationRequest(id, nonce, expiresAtUtc);
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

    internal void Invalidate(UtcDateTimeOffset utcNow)
    {
        // Idempotent — also blocks invalidating an already-consumed request to prevent
        // overwriting ConsumedAtUtc with a later timestamp.
        if (InvalidatedAtUtc is not null || ConsumedAtUtc is not null)
        {
            return;
        }

        InvalidatedAtUtc = utcNow;
    }

    internal void Consume(UtcDateTimeOffset utcNow)
    {
        // Idempotent — email confirmation links may be followed more than once.
        if (ConsumedAtUtc is not null)
        {
            return;
        }

        ConsumedAtUtc = utcNow;
    }

}
