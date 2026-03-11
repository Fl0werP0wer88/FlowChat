using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.UserProfiles.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandHandler
    : CommandHandlerBase<CreateInitialUserProfileCommand, Guid>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private UserProfile? _userProfile;

    public CreateInitialUserProfileCommandHandler(
        IUserProfileRepository userProfileRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileRepository = userProfileRepository;
    }

    protected override async Task<Result<Guid, IDomainError>> ExecuteAsync(
        CreateInitialUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("UserName is required."));
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Validation("DisplayName is required."));
        }

        var userName = request.UserName.Trim();
        var displayName = request.DisplayName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();

        if (email is null && phone is null)
        {
            return Result.Failure<Guid, IDomainError>(
                DomainError.Validation("At least one email or phone is required to create a user profile."));
        }

        var exists = await _userProfileRepository
            .UserNameExistsAsync(userName, cancellationToken: cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid, IDomainError>(DomainError.Conflict($"UserName '{userName}' already exists."));
        }

        var userProfileId = Id<UserProfile>.FromGuid(request.UserId);
        List<Email> emails = email is null
            ? []
            : [Email.Create(userProfileId, email, isMain: true)];
        List<Phone> phones = phone is null
            ? []
            : [Phone.Create(userProfileId, phone, isMain: true)];

        _userProfile = UserProfile.Create(
            userName,
            displayName,
            request.AvatarUrl,
            request.Bio,
            emails: emails,
            phones: phones,
            id: userProfileId);

        await _userProfileRepository.AddAsync(_userProfile, cancellationToken);

        return _userProfile.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(Result<Guid, IDomainError> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
