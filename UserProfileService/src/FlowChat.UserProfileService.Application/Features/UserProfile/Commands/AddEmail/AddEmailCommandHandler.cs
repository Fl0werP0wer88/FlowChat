using FlowChat.Shared.Application;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using DomainEmail = FlowChat.UserProfileService.Domain.Entities.UserProfile.Email;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddEmail;

public sealed class AddEmailCommandHandler
    : IdempotentCommandHandlerBase<AddEmailCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public AddEmailCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<(bool Found, Guid Value)> TryGetExistingResponseAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        var email = userProfile?.Emails.FirstOrDefault(x => x.Id.Value == request.EmailId);

        return email is null
            ? (false, default)
            : (true, email.Id.Value);
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteCommandAsync(
        AddEmailCommand request,
        CancellationToken cancellationToken)
    {
        if (!EmailAddress.TryCreate(request.Address!, out var normalizedEmailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        if (await _userProfileReadRepository.EmailAddressExistsAsync(normalizedEmailAddress!.Value, cancellationToken))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{normalizedEmailAddress.Value}' is already taken."));
        }

        var email = _userProfile.AddEmail(Id<DomainEmail>.FromGuid(request.EmailId), normalizedEmailAddress);

        return FlowChatResult<Guid>.Success(email.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot() =>
        _userProfile;

    protected override string GetIdempotencyConflictKey(AddEmailCommand request) =>
        AddEmailCommand.IdempotencyConflictKey;
}
