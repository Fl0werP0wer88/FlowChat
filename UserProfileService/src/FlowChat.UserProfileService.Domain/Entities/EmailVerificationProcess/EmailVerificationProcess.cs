using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using DomainEmailVerificationRequest = FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest.EmailVerificationRequest;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using DomainUserProfile = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess;

public sealed class EmailVerificationProcess : AggregateRootBase<EmailVerificationProcess>
{
    private readonly List<DomainEmailVerificationRequest> _requests = [];

    private EmailVerificationProcess() : base(Id<EmailVerificationProcess>.New())
    {
    }

    private EmailVerificationProcess(
        Id<EmailVerificationProcess> id,
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId) : base(id)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(emailId);

        UserProfileId = userProfileId;
        EmailId = emailId;
    }

    public Id<DomainUserProfile> UserProfileId { get; private set; } = default!;
    public Id<DomainEmail> EmailId { get; private set; } = default!;
    public IReadOnlyList<DomainEmailVerificationRequest> Requests => _requests.AsReadOnly();

    public static EmailVerificationProcess Create(
        Id<DomainUserProfile> userProfileId,
        Id<DomainEmail> emailId)
    {
        ArgumentNullException.ThrowIfNull(userProfileId);
        ArgumentNullException.ThrowIfNull(emailId);

        return new EmailVerificationProcess(
            Id<EmailVerificationProcess>.FromGuid(emailId.Value),
            userProfileId,
            emailId);
    }

    public DomainEmailVerificationRequest IssueRequest(
        Id<DomainEmailVerificationRequest> requestId,
        string nonce,
        UtcDateTimeOffset expiresAtUtc,
        UtcDateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(requestId);

        foreach (var activeRequest in _requests.Where(x => x.IsActive(utcNow)))
        {
            activeRequest.Invalidate(utcNow);
        }

        var request = DomainEmailVerificationRequest.Create(requestId, nonce, expiresAtUtc);
        _requests.Add(request);
        IncrementVersion();

        EnsureSingleActiveRequest(utcNow);

        return request;
    }

    public bool TryGetRequestByNonce(string nonce, out DomainEmailVerificationRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nonce);

        request = _requests.FirstOrDefault(x => x.Nonce == nonce.Trim())!;
        return request is not null;
    }

    public void ConsumeRequest(string nonce, UtcDateTimeOffset utcNow)
    {
        if (!TryGetRequestByNonce(nonce, out var request))
        {
            throw new InvalidOperationException("Email verification request was not found.");
        }

        request.Consume(utcNow);
        IncrementVersion();
    }

    private void EnsureSingleActiveRequest(UtcDateTimeOffset utcNow)
    {
        if (_requests.Count(x => x.IsActive(utcNow)) > 1)
        {
            throw new InvalidOperationException("Email verification process cannot have more than one active request.");
        }
    }
}
