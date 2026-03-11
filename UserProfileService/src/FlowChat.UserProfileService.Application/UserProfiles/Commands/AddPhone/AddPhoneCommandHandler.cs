using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.AddPhone;

public sealed class AddPhoneCommandHandler
    : CommandHandlerBase<AddPhoneCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public AddPhoneCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        AddPhoneCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("UserId is required."));
        }

        if (string.IsNullOrWhiteSpace(request.Number))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("Phone number is required."));
        }

        _userProfile = await _userProfileRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var normalizedNumber = request.Number!.Trim();
        if (_userProfile.Phones.Any(x => string.Equals(x.Number, normalizedNumber, StringComparison.OrdinalIgnoreCase)))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Conflict($"Phone '{normalizedNumber}' already exists."));
        }

        var phone = _userProfile.AddPhone(normalizedNumber);

        return phone.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
