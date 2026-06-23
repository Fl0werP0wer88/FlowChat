using FlowChat.Shared.Application;
using FlowChat.Core.Results;
using FlowChat.Shared.Domain;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using FlowChat.Shared.Domain.ValueObjects;
using DomainPhone = FlowChat.UserProfileService.Domain.Entities.UserProfile.Phone;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.AddPhone;

public sealed class AddPhoneCommandHandler
    : IdempotentCommandHandlerBase<AddPhoneCommand, Guid>
{
    private readonly IUserProfileWriteRepository _userProfileRepository;
    private UserProfileAggregate? _userProfile;

    public AddPhoneCommandHandler(
        IUserProfileWriteRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IDbUpdateExceptionClassifier dbUpdateExceptionClassifier)
        : base(domainEventDispatcher, unitOfWork, dbUpdateExceptionClassifier)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<(bool Found, Guid Value)> TryGetExistingResponseAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        var userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        var phone = userProfile?.Phones.FirstOrDefault(x => x.Id.Value == request.PhoneId);

        return phone is null
            ? (false, default)
            : (true, phone.Id.Value);
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteCommandAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        if (!PhoneNumber.TryCreate(request.Number!, out var normalizedPhoneNumber))
        {
            throw new InvalidOperationException("Validated phone number could not be normalized.");
        }

        _userProfile = await _userProfileRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        if (_userProfile.Phones.Any(x => x.Number == normalizedPhoneNumber))
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Phone '{normalizedPhoneNumber!.Value}' already exists."));
        }

        var phone = _userProfile.AddPhone(Id<DomainPhone>.FromGuid(request.PhoneId), normalizedPhoneNumber!);

        return FlowChatResult<Guid>.Success(phone.Id.Value);
    }

    protected override IAggregateRoot? GetExecutedAggregateRoot(IdempotentCommandResult<Guid> result) =>
        _userProfile;

    protected override string GetIdempotencyConflictKey(AddPhoneCommand request) =>
        AddPhoneCommand.IdempotencyConflictKey;
}

