using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using MediatR;
using Microsoft.EntityFrameworkCore;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandHandler
    : CommandHandlerBase<ConfirmEmailVerificationCommand, IdempotentCommandResult<Unit>>
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

    protected override async Task<FlowChatResult<IdempotentCommandResult<Unit>>> ExecuteAsync(
        ConfirmEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_emailVerificationTokenProtector.TryUnprotect(request.Token, out var payload) || payload is null)
        {
            return ValidationFailure();
        }

        var verificationRequest = await _emailVerificationRequestWriteRepository
            .GetByNonceAsync(payload.Nonce, cancellationToken);

        // Validate IDs from the payload against the stored request before checking IsActive —
        // a tampered token that maps to a real nonce but wrong IDs must be rejected early.
        if (verificationRequest is null
            || verificationRequest.UserProfileId.Value != payload.UserProfileId
            || verificationRequest.EmailId.Value != payload.EmailId)
        {
            return ValidationFailure();
        }

        var nowUtc = UtcDateTimeOffset.UtcNow;
        if (verificationRequest.InvalidatedAtUtc is not null)
        {
            return ValidationFailure();
        }

        if (verificationRequest.ConsumedAtUtc is null && verificationRequest.IsExpired(nowUtc))
        {
            return ValidationFailure();
        }

        _userProfile = await _userProfileWriteRepository.GetByIdAsync(payload.UserProfileId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<IdempotentCommandResult<Unit>>.Failure(
                DomainError.NotFound($"User profile '{payload.UserProfileId}' was not found."));
        }

        var email = _userProfile.Emails.FirstOrDefault(x => x.Id.Value == payload.EmailId);
        if (email is null)
        {
            return FlowChatResult<IdempotentCommandResult<Unit>>.Failure(
                DomainError.NotFound($"Email '{payload.EmailId}' was not found for user profile '{payload.UserProfileId}'."));
        }

        if (verificationRequest.ConsumedAtUtc is not null)
        {
            return email.IsConfirmed
                ? Success(wasAlreadyProcessed: true)
                : ValidationFailure();
        }

        _userProfile.ConfirmEmail(email.Id);
        verificationRequest.Consume(nowUtc);

        return Success(wasAlreadyProcessed: false);
    }

    protected override async Task<FlowChatResult<IdempotentCommandResult<Unit>>> OnDbUpdateExceptionAfterRollbackHook(
        ConfirmEmailVerificationCommand request,
        DbUpdateException exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException
            || !_emailVerificationTokenProtector.TryUnprotect(request.Token, out var payload)
            || payload is null)
        {
            return await base.OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken);
        }

        var confirmationState = await _emailVerificationRequestWriteRepository
            .GetConfirmationStateByNonceAsync(payload.Nonce, cancellationToken);

        if (IsConfirmedBySameToken(payload, confirmationState))
        {
            return Success(wasAlreadyProcessed: true);
        }

        return await base.OnDbUpdateExceptionAfterRollbackHook(request, exception, cancellationToken);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<IdempotentCommandResult<Unit>> result)
    {
        return result.IsSuccess && !result.Value.WasAlreadyProcessed ? _userProfile : null;
    }

    private static bool IsConfirmedBySameToken(
        EmailVerificationTokenPayload payload,
        EmailVerificationConfirmationState? confirmationState)
    {
        return confirmationState is not null
            && confirmationState.UserProfileId == payload.UserProfileId
            && confirmationState.EmailId == payload.EmailId
            && confirmationState.InvalidatedAtUtc is null
            && confirmationState.ConsumedAtUtc is not null
            && confirmationState.EmailIsConfirmed;
    }

    private static FlowChatResult<IdempotentCommandResult<Unit>> Success(bool wasAlreadyProcessed)
    {
        return FlowChatResult<IdempotentCommandResult<Unit>>.Success(
            new IdempotentCommandResult<Unit>(Unit.Value, wasAlreadyProcessed));
    }

    private static FlowChatResult<IdempotentCommandResult<Unit>> ValidationFailure()
    {
        return FlowChatResult<IdempotentCommandResult<Unit>>.Failure(
            DomainError.Validation(InvalidTokenMessage));
    }
}
