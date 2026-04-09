using FlowChat.Shared.Application;
using FlowChat.Shared.Domain;
using FlowChat.Shared.Domain.ValueObjects;
using FlowChat.SocialGraphService.Application.Contracts.Persistence;
using FlowChat.SocialGraphService.Application.Features.UserProfile;
using ContactAggregate = FlowChat.SocialGraphService.Domain.Entities.Contact.Contact;

namespace FlowChat.SocialGraphService.Application.Features.Contact.Commands.AddContact;

public sealed class AddContactCommandHandler : CommandHandlerBase<AddContactCommand, Guid>
{
    private readonly IContactWriteRepository _contactWriteRepository;
    private readonly IUserProfileProjectionReadRepository _userProfileProjectionReadRepository;

    public AddContactCommandHandler(
        IContactWriteRepository contactWriteRepository,
        IUserProfileProjectionReadRepository userProfileProjectionReadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher) : base(domainEventDispatcher, unitOfWork)
    {
        _contactWriteRepository = contactWriteRepository;
        _userProfileProjectionReadRepository = userProfileProjectionReadRepository;
    }

    protected override async Task<FlowChatResult<Guid>> ExecuteAsync(
        AddContactCommand request,
        CancellationToken cancellationToken)
    {
        var projection = await GetUserProfileProjectionAsync(request, cancellationToken);
        if (projection is null)
        {
            return FlowChatResult<Guid>.Failure(DomainError.NotFound("User profile projection was not found."));
        }

        if (projection.UserProfileId == request.OwnerUserId)
        {
            return FlowChatResult<Guid>.Failure(DomainError.BadRequest("OwnerUserId and ContactUserId must be different."));
        }

        var contactAlreadyExists = await _contactWriteRepository.ExistsAsync(
            request.OwnerUserId,
            projection.UserProfileId,
            cancellationToken);

        if (contactAlreadyExists)
        {
            return FlowChatResult<Guid>.Failure(DomainError.Conflict("Contact already exists."));
        }

        var contact = ContactAggregate.Create(
            request.OwnerUserId,
            projection.UserProfileId,
            projection.DisplayName,
            projection.FirstName,
            projection.LastName,
            CreatePhoneNumber(projection),
            CreateEmailAddress(projection));

        await _contactWriteRepository.AddAsync(contact, cancellationToken);

        return FlowChatResult<Guid>.Success(contact.Id.Value);
    }

    protected override IAggregateRoot? GetAggregateRoot(FlowChatResult<Guid> result) => null;

    private async Task<UserProfileProjection?> GetUserProfileProjectionAsync(
        AddContactCommand request,
        CancellationToken cancellationToken)
    {
        if (request.UserId.HasValue)
        {
            return await _userProfileProjectionReadRepository.GetByUserProfileIdAsync(
                request.UserId.Value,
                cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.FriendlyUserId))
        {
            return await _userProfileProjectionReadRepository.GetByFriendlyUserIdAsync(
                request.FriendlyUserId.Trim(),
                cancellationToken);
        }

        return await _userProfileProjectionReadRepository.GetByEmailAsync(
            EmailAddress.Create(request.Email!).Value,
            cancellationToken);
    }

    private static EmailAddress? CreateEmailAddress(UserProfileProjection projection)
    {
        if (!EmailAddress.TryCreate(projection.MainEmail, out var emailAddress))
        {
            return null;
        }

        return emailAddress;
    }

    private static PhoneNumber? CreatePhoneNumber(UserProfileProjection projection)
    {
        if (!PhoneNumber.TryCreate(projection.MainPhone, out var phoneNumber))
        {
            return null;
        }

        return phoneNumber;
    }
}
