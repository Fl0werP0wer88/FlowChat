using CSharpFunctionalExtensions;
using FlowChat.Application.Abstractions;
using FlowChat.Domain.Abstractions;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;
using FlowChat.Domain.Abstractions.ValueObjects;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandHandler
    : CommandHandlerBase<CreateInitialUserProfileCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private UserProfile? _userProfile;

    public CreateInitialUserProfileCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileWriteRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileWriteRepository = userProfileWriteRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        CreateInitialUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = new ValidationErrorCollector()
            .AddIf(string.IsNullOrWhiteSpace(request.UserName), "UserName is required.")
            .AddIf(string.IsNullOrWhiteSpace(request.DisplayName), "DisplayName is required.");

        var userName = string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        EmailAddress? emailAddress = null;
        PhoneNumber? phoneNumber = null;

        validationErrors.AddIf(email is null && phone is null, "At least one email or phone is required to create a user profile.");

        if (email is not null && !EmailAddress.TryCreate(email, out emailAddress))
        {
            validationErrors.AddIf(true, EmailAddress.InvalidEmailAddressMessage);
        }

        if (phone is not null && !PhoneNumber.TryCreate(phone, out phoneNumber))
        {
            validationErrors.AddIf(true, PhoneNumber.InvalidPhoneNumberMessage);
        }

        if (validationErrors.HasErrors)
        {
            return validationErrors.ToFailure<Guid>();
        }

        var exists = await _userProfileReadRepository
            .UserNameExistsAsync(userName!, cancellationToken: cancellationToken);

        if (exists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"UserName '{userName}' already exists."));
        }

        var userProfileId = Id<UserProfile>.FromGuid(request.UserId);
        List<Email> emails = email is null
            ? []
            : [Email.Create(userProfileId, emailAddress!, isMain: true)];
        List<Phone> phones = phone is null
            ? []
            : [Phone.Create(userProfileId, phoneNumber!, isMain: true)];

        _userProfile = UserProfile.Create(
            userName!,
            displayName!,
            request.AvatarUrl,
            request.Bio,
            emails: emails,
            phones: phones,
            id: userProfileId);

        await _userProfileWriteRepository.AddAsync(_userProfile, cancellationToken);

        return _userProfile.Id.Value;
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
