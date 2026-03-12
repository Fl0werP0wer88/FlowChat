using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.UserProfileService.Domain.Events;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.SetMainPhone;

public sealed class SetMainPhoneCommandHandler
    : CommandHandlerBase<SetMainPhoneCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public SetMainPhoneCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        SetMainPhoneCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("UserId is required."));
        }

        if (request.PhoneId == Guid.Empty)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("PhoneId is required."));
        }

        _userProfile = await _userProfileRepository.GetByIdForUpdateAsync(request.UserId, cancellationToken);
        if (_userProfile is null)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.NotFound($"User profile '{request.UserId}' was not found."));
        }

        var phone = _userProfile.Phones.FirstOrDefault(x => x.Id.Value == request.PhoneId);
        if (phone is null)
        {
            return Result.Failure<Guid, IDomainError>(
                DomainError.NotFound($"Phone '{request.PhoneId}' was not found for user profile '{request.UserId}'."));
        }

        _userProfile.SetMainPhone(phone.Id);

        return phone.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
