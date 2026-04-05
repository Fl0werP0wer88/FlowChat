using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using MediatR;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandHandler
    : CommandHandlerBase<ConfirmEmailVerificationCommand, Unit>
{
    // Single generic message for all token failure cases — prevents callers from probing
    // whether a token exists, has been consumed, or belongs to a different user.
    private const string InvalidTokenMessage = "Email verification link is invalid or has expired.";

    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IEmailVerificationRequestWriteRepository _emailVerificationRequestWriteRepository;
    private readonly IEmailVerificationTokenProtector _emailVerificationTokenProtector;
    private UserProfileAggregate? _userProfile;

    public ConfirmEmailVerificationCommandHandler(
        IUserProfileWriteRepository userProfileWriteRepository,
        IEmailVerificationRequestWriteRepository emailVerificationRequestWriteRepository,
        IEmailVerificationTokenProtector emailVerificationTokenProtector,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _emailVerificationRequestWriteRepository = emailVerificationRequestWriteRepository;
        _emailVerificationTokenProtector = emailVerificationTokenProtector;
    }

    protected override async Task<FlowChatResult<Unit>> ExecuteAsync(
        ConfirmEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_emailVerificationTokenProtector.TryUnprotect(request.Token, out var payload) || payload is null)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Validation(InvalidTokenMessage));
        }

        var verificationRequest = await _emailVerificationRequestWriteRepository
            .GetByNonceAsync(payload.Nonce, cancellationToken);

        // Validate IDs from the payload against the stored request before checking IsActive —
        // a tampered token that maps to a real nonce but wrong IDs must be rejected early.
        if (verificationRequest is null
            || verificationRequest.UserProfileId.Value != payload.UserProfileId
            || verificationRequest.EmailId.Value != payload.EmailId)
        {
            return FlowChatResult<Unit>.Failure(DomainError.Validation(InvalidTokenMessage));
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (!verificationRequest.IsActive(nowUtc))
        {
            return FlowChatResult<Unit>.Failure(DomainError.Validation(InvalidTokenMessage));
        }

        _userProfile = await _userProfileWriteRepository.GetByIdAsync(payload.UserProfileId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Unit>.Failure(DomainError.NotFound($"User profile '{payload.UserProfileId}' was not found."));
        }

        var email = _userProfile.Emails.FirstOrDefault(x => x.Id.Value == payload.EmailId);
        if (email is null)
        {
            return FlowChatResult<Unit>.Failure(
                DomainError.NotFound($"Email '{payload.EmailId}' was not found for user profile '{payload.UserProfileId}'."));
        }

        _userProfile.ConfirmEmail(email.Id);
        verificationRequest.Consume(nowUtc);

        return FlowChatResult<Unit>.Success(Unit.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Unit> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
