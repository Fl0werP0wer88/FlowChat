using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.UserProfileService.Application.Contracts.Persistence;
using FlowChat.UserProfileService.Domain.Entities.EmailVerificationRequest;
using UserProfileAggregate = FlowChat.UserProfileService.Domain.Entities.UserProfile.UserProfile;

namespace FlowChat.UserProfileService.Application.Features.UserProfile.Commands.CreateInitialUserProfile;

public sealed class CreateInitialUserProfileCommandHandler
    : CommandHandlerBase<CreateInitialUserProfileCommand, Guid>
{
    private readonly IUserProfileReadRepository _userProfileReadRepository;
    private readonly IUserProfileWriteRepository _userProfileWriteRepository;
    private UserProfileAggregate? _userProfile;

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
        var friendlyUserId = string.IsNullOrWhiteSpace(request.FriendlyUserId) ? null : request.FriendlyUserId.Trim();
        var firstName = NormalizeOptional(request.FirstName);
        var lastName = NormalizeOptional(request.LastName);
        var organization = NormalizeOptional(request.Organization);
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        EmailAddress? emailAddress = null;

        if (email is not null && !EmailAddress.TryCreate(email, out emailAddress))
        {
            throw new InvalidOperationException("Validated email address could not be normalized.");
        }

        var friendlyUserIdExists = await _userProfileReadRepository
            .FriendlyUserIdExistsAsync(friendlyUserId!, cancellationToken: cancellationToken);

        if (friendlyUserIdExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"FriendlyUserId '{friendlyUserId}' already exists."));
        }

        var emailExists = await _userProfileReadRepository
            .EmailAddressExistsAsync(emailAddress!.Value, cancellationToken);

        if (emailExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict($"Email '{emailAddress.Value}' already exists."));
        }

        var userProfileId = Id<UserProfileAggregate>.FromGuid(request.UserId);
        _userProfile = UserProfileAggregate.Create(
            userProfileId,
            friendlyUserId!,
            emailAddress!,
            firstName: firstName,
            lastName: lastName,
            organization: organization);

        await _userProfileWriteRepository.AddAsync(_userProfile, cancellationToken);

        return FlowChatResult<Guid>.Success(_userProfile.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result)
    {
        return result.IsSuccess ? _userProfile : null;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
