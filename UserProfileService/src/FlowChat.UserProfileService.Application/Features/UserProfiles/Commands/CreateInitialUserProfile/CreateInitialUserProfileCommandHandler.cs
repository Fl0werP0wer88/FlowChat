using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Infrastructure;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities;

namespace FlowChat.UserProfileService.Application.Features.UserProfiles.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandHandler
    : CommandHandlerBase<CreateInitialUserProfileCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private readonly IEmailVerificationRequestIssuer _emailVerificationRequestIssuer;
    private UserProfile? _userProfile;

    public CreateInitialUserProfileCommandHandler(
        IUserProfileReadRepository userProfileReadRepository,
        IUserProfileWriteRepository userProfileWriteRepository,
        IEmailVerificationRequestIssuer emailVerificationRequestIssuer,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _userProfileReadRepository = userProfileReadRepository;
        _userProfileWriteRepository = userProfileWriteRepository;
        _emailVerificationRequestIssuer = emailVerificationRequestIssuer;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        CreateInitialUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? null : request.UserName.Trim();
        var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        EmailAddress? emailAddress = null;
        PhoneNumber? phoneNumber = null;

        if (email is not null && !EmailAddress.TryCreate(email, out emailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        if (phone is not null && !PhoneNumber.TryCreate(phone, out phoneNumber))
        {
            throw new InvalidOperationException("Validated phone number could not be normalized.");
        }

        var userNameExists = await _userProfileReadRepository
            .UserNameExistsAsync(userName!, cancellationToken: cancellationToken);

        if (userNameExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"UserName '{userName}' already exists."));
        }

        var emailExists = await _userProfileReadRepository
            .EmailAddressExistsAsync(emailAddress!.Value, cancellationToken);

        if (emailExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{emailAddress.Value}' already exists."));
        }

        var userProfileId = Id<UserProfile>.FromGuid(request.UserId);
        List<Email> emails =
        [
            Email.Create(userProfileId, emailAddress!, isMain: true, isAuth: true)
        ];
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
        await _emailVerificationRequestIssuer.IssueAsync(_userProfile, _userProfile.Emails.Single(), cancellationToken);

        return FlowChatResult<Guid>.Success(_userProfile.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }
}
