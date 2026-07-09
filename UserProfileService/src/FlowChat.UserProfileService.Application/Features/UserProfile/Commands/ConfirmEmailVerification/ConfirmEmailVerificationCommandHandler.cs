using FlowChat.Core.Results;
using FlowChat.Shared.Application;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2;
using FlowChat.Shared.Application.CommandHandlers.AggregateRootCommandHandlerBaseV2.BeforeSaveProcessors;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Application.Features.UserProfile.EmailVerification;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainEmailVerificationProcess = FlowChat.UserProfileService.Domain.Entities.EmailVerificationProcess.EmailVerificationProcess;
using DomainEmailVerificationRequest = FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest.EmailVerificationRequest;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.ConfirmEmailVerification;

public sealed class ConfirmEmailVerificationCommandHandler
    : AggregateRootUpdateCommandHandlerBaseV3<ConfirmEmailVerificationCommand, IdempotentCommandResult<Unit>, UserProfileAggregate>
{
    // Single generic message for all token failure cases — prevents callers from probing
    // whether a token exists, has been consumed, or belongs to a different user.
    private const string InvalidTokenMessage = "Email verification link is invalid or has expired.";

    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IEmailVerificationProcessWriteRepository _emailVerificationProcessWriteRepository;
    private readonly IEmailVerificationTokenProtector _emailVerificationTokenProtector;
    private EmailVerificationTokenPayload? _payload;
    private DomainEmailVerificationProcess? _process;
    private DomainEmailVerificationRequest? _verificationRequest;
    private UtcDateTimeOffset? _nowUtc;

    public ConfirmEmailVerificationCommandHandler(
        IUserProfileWriteRepository userProfileWriteRepository,
        IEmailVerificationProcessWriteRepository emailVerificationProcessWriteRepository,
        IEmailVerificationTokenProtector emailVerificationTokenProtector,
        IUnitOfWork unitOfWork,
        ILocalEventDispatcher domainEventDispatcher,
        IEnumerable<IAggregateBeforeSaveProcessor<ConfirmEmailVerificationCommand, UserProfileAggregate>> beforeSaveProcessors)
        : base(domainEventDispatcher, unitOfWork, beforeSaveProcessors)
    {
        _userProfileWriteRepository = userProfileWriteRepository;
        _emailVerificationProcessWriteRepository = emailVerificationProcessWriteRepository;
        _emailVerificationTokenProtector = emailVerificationTokenProtector;
    }

    protected override async Task<FlowChatResult<UserProfileAggregate?>> FetchAggregateRootAsync(
        ConfirmEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!_emailVerificationTokenProtector.TryUnprotect(request.Token, out var payload) || payload is null)
        {
            return AggregateValidationFailure();
        }

        _payload = payload;

        var process = await _emailVerificationProcessWriteRepository
            .GetByNonceAsync(payload.Nonce, cancellationToken);

        // Validate IDs from the payload against the stored request before checking IsActive —
        // a tampered token that maps to a real nonce but wrong IDs must be rejected early.
        if (process is null
            || process.UserProfileId.Value != payload.UserProfileId
            || process.EmailId.Value != payload.EmailId
            || !process.TryGetRequestByNonce(payload.Nonce, out var verificationRequest))
        {
            return AggregateValidationFailure();
        }

        _process = process;
        _verificationRequest = verificationRequest;

        var nowUtc = UtcDateTimeOffset.UtcNow;
        _nowUtc = nowUtc;
        if (verificationRequest.InvalidatedAtUtc is not null)
        {
            return AggregateValidationFailure();
        }

        if (verificationRequest.ConsumedAtUtc is null && verificationRequest.IsExpired(nowUtc))
        {
            return AggregateValidationFailure();
        }

        var userProfile = await _userProfileWriteRepository.GetByIdAsync(payload.UserProfileId, cancellationToken);
        if (userProfile is null)
        {
            return FlowChatResult<UserProfileAggregate?>.Failure(
                DomainError.NotFound($"User profile '{payload.UserProfileId}' was not found."));
        }

        return FlowChatResult<UserProfileAggregate?>.Success(userProfile);
    }

    protected override Task<FlowChatResult<IdempotentCommandResult<Unit>>> ExecuteAsync(
        ConfirmEmailVerificationCommand request,
        CancellationToken cancellationToken)
    {
        var email = AggregateRoot!.Emails.FirstOrDefault(x => x.Id.Value == _payload!.EmailId);
        if (email is null)
        {
            return Task.FromResult(FlowChatResult<IdempotentCommandResult<Unit>>.Failure(
                DomainError.NotFound($"Email '{_payload!.EmailId}' was not found for user profile '{_payload.UserProfileId}'.")));
        }

        if (_verificationRequest!.ConsumedAtUtc is not null)
        {
            return Task.FromResult(email.IsConfirmed
                ? Success(wasAlreadyProcessed: true)
                : ValidationFailure());
        }

        AggregateRoot.ConfirmEmail(email.Id);
        _process!.ConsumeRequest(_payload!.Nonce, _nowUtc!.Value);
        SetUpdated();

        return Task.FromResult(Success(wasAlreadyProcessed: false));
    }

    protected override async Task<FlowChatResult<IdempotentCommandResult<Unit>>> HandleUnexpectedExceptionAsync(
        ConfirmEmailVerificationCommand request,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException
            || !_emailVerificationTokenProtector.TryUnprotect(request.Token, out var payload)
            || payload is null)
        {
            return await base.HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
        }

        var confirmationState = await _emailVerificationProcessWriteRepository
            .GetConfirmationStateByNonceAsync(payload.Nonce, cancellationToken);

        if (IsConfirmedBySameToken(payload, confirmationState))
        {
            return Success(wasAlreadyProcessed: true);
        }

        return await base.HandleUnexpectedExceptionAsync(request, exception, cancellationToken);
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

    private static FlowChatResult<UserProfileAggregate?> AggregateValidationFailure()
    {
        return FlowChatResult<UserProfileAggregate?>.Failure(
            DomainError.Validation(InvalidTokenMessage));
    }
}
